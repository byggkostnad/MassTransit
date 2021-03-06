﻿// Copyright 2007-2016 Chris Patterson, Dru Sellers, Travis Smith, et. al.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace MassTransit.AutomatonymousIntegration.Tests
{
    using System;
    using System.Threading.Tasks;
    using Automatonymous;
    using NUnit.Framework;
    using Saga;
    using TestFramework;


    [TestFixture]
    public class InMemoryDeadlock_Specs :
        StateMachineTestFixture
    {
        [Test]
        public async Task Should_not_deadlock_on_the_repository()
        {
            var id = NewId.NextGuid();

            await InputQueueSendEndpoint.Send<CreateInstance>(new
            {
                CorrelationId = id
            });

            Guid? saga = await _repository.ShouldContainSaga(state => state.CorrelationId == id && state.CurrentState == _machine.Active, TestTimeout);

            Assert.IsTrue(saga.HasValue);

            await InputQueueSendEndpoint.Send<CompleteInstance>(new
            {
                CorrelationId = id
            });

            await Task.Delay(990);

            await Console.Out.WriteLineAsync("Sending duplicate message");

            await InputQueueSendEndpoint.Send<CancelInstance>(new
            {
                CorrelationId = id
            });

            id = NewId.NextGuid();
            await InputQueueSendEndpoint.Send<CreateInstance>(new
            {
                CorrelationId = id
            });
            Guid? saga2 = await _repository.ShouldContainSaga(state => state.CorrelationId == id && state.CurrentState == _machine.Active, TestTimeout);
            Assert.IsTrue(saga2.HasValue);
        }

        InMemorySagaRepository<Instance> _repository;
        DeadlockStateMachine _machine;

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            base.ConfigureInMemoryReceiveEndpoint(configurator);

            _repository = new InMemorySagaRepository<Instance>();
            _machine = new DeadlockStateMachine();

            configurator.StateMachineSaga(_machine, _repository);
        }


        class Instance :
            SagaStateMachineInstance
        {
            public State CurrentState { get; set; }
            public Guid CorrelationId { get; set; }
        }


        class DeadlockStateMachine :
            MassTransitStateMachine<Instance>
        {
            public DeadlockStateMachine()
            {
                InstanceState(x => x.CurrentState);

                Initially(
                    When(Create)
                        .TransitionTo(Active));

                During(Active,
                    When(Complete)
                        .ThenAsync(async context =>
                        {
                            await Console.Out.WriteLineAsync($"Completing: {context.Instance.CorrelationId}");
                            await Task.Delay(1000);
                            await Console.Out.WriteLineAsync($"Completed: {context.Instance.CorrelationId}");
                        })
                        .Finalize(),
                    When(Cancel)
                        .ThenAsync(async context =>
                        {
                            await Console.Out.WriteLineAsync($"Canceling: {context.Instance.CorrelationId}");
                            await Task.Delay(1000);
                            await Console.Out.WriteLineAsync($"Canceled: {context.Instance.CorrelationId}");
                        })
                        .Finalize());

                SetCompletedWhenFinalized();
            }

            public State Active { get; private set; }
            public Event<CreateInstance> Create { get; private set; }
            public Event<CompleteInstance> Complete { get; private set; }
            public Event<CancelInstance> Cancel { get; private set; }
        }


        public interface CreateInstance :
            CorrelatedBy<Guid>
        {
        }


        public interface CompleteInstance :
            CorrelatedBy<Guid>
        {
        }


        public interface CancelInstance :
            CorrelatedBy<Guid>
        {
        }
    }
}