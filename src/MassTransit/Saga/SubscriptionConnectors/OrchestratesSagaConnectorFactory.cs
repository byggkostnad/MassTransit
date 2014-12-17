// Copyright 2007-2014 Chris Patterson, Dru Sellers, Travis Smith, et. al.
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
namespace MassTransit.Saga.SubscriptionConnectors
{
    using System;
    using Pipeline.Filters;


    public class OrchestratesSagaConnectorFactory<TSaga, TMessage> :
        SagaConnectorFactory
        where TSaga : class, ISaga, Orchestrates<TMessage>
        where TMessage : class, CorrelatedBy<Guid>
    {
        readonly OrchestratesSagaMessageFilter<TSaga, TMessage> _consumeFilter;
        readonly SagaLocatorFilter<TSaga, TMessage> _locatorFilter;

        public OrchestratesSagaConnectorFactory()
        {
            var policy = new ExistingOrIgnoreSagaPolicy<TSaga, TMessage>(x => false);

            _consumeFilter = new OrchestratesSagaMessageFilter<TSaga, TMessage>();
            var locator = new CorrelationIdSagaLocator<TMessage>(x => x.Message.CorrelationId);

            _locatorFilter = new SagaLocatorFilter<TSaga, TMessage>(locator, policy);
        }

        public SagaMessageConnector CreateMessageConnector()
        {
            return new CorrelatedSagaMessageConnector<TSaga, TMessage>(_consumeFilter, _locatorFilter);
        }
    }
}