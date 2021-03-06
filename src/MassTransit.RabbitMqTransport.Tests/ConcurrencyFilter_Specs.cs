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
namespace MassTransit.RabbitMqTransport.Tests
{
    using System.Threading;
    using System.Threading.Tasks;
    using GreenPipes;
    using NUnit.Framework;


    [TestFixture]
    public class Using_a_consumer_concurrency_limit :
        RabbitMqTestFixture
    {
        [Test]
        public async Task Should_limit_the_consumer()
        {
            _complete = GetTask<bool>();

            for (var i = 0; i < _messageCount; i++)
            {
                Bus.Publish(new A());
            }

            await _complete.Task;

            Assert.AreEqual(1, _consumer.MaxDeliveryCount);
        }

        Consumer _consumer;
        static int _messageCount = 100;
        static TaskCompletionSource<bool> _complete;

        protected override void ConfigureRabbitMqReceiveEndoint(IRabbitMqReceiveEndpointConfigurator configurator)
        {
            base.ConfigureRabbitMqReceiveEndoint(configurator);

            _consumer = new Consumer();

            configurator.Consumer(() => _consumer, x => x.UseConcurrencyLimit(1));
        }


        class Consumer :
            IConsumer<A>
        {
            int _currentPendingDeliveryCount;
            long _deliveryCount;
            int _maxPendingDeliveryCount;

            public int MaxDeliveryCount
            {
                get { return _maxPendingDeliveryCount; }
            }

            public async Task Consume(ConsumeContext<A> context)
            {
                Interlocked.Increment(ref _deliveryCount);

                var current = Interlocked.Increment(ref _currentPendingDeliveryCount);
                while (current > _maxPendingDeliveryCount)
                    Interlocked.CompareExchange(ref _maxPendingDeliveryCount, current, _maxPendingDeliveryCount);

                await Task.Delay(100);

                Interlocked.Decrement(ref _currentPendingDeliveryCount);

                if (_deliveryCount == _messageCount)
                    _complete.TrySetResult(true);
            }
        }


        class A
        {
        }
    }
}