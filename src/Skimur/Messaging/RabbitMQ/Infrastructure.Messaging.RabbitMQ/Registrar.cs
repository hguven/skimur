﻿using System;
using System.Configuration;
using ServiceStack;
using ServiceStack.RabbitMq;
using SimpleInjector;
using Skimur;
using Skimur.Logging;
using Skimur.Messaging;

namespace Infrastructure.Messaging.RabbitMQ
{
    public class Registrar : IRegistrar
    {
        public void Register(Container container)
        {
            container.RegisterSingleton(() =>
            {
                LicenseUtils.RegisterLicense("2283-e1JlZjoyMjgzLE5hbWU6TWVkWENoYW5nZSxUeXBlOkluZGllLEhhc2g6TU" +
                "FyaTVzNGdQcEdlc0pqd1ZIUXVlL0lacDBZcCt3TkFLY0UyMTlJblBuMzRLNWFRb" +
                "HBYN204aGkrQXlRYzUvZnNVUlZzWXd4NjR0OFlXZEpjNUNYRTdnMjBLR0ZjQmhG" +
                "dTFNMHZVazJqcHdQb1RrbStDaHNPRm11Qm50TnZzOTkwcHAzRkxtTC9idThMekN" +
                "lTVRndFBORzBuREZ0WGJUdzdRMi80K09lQ2tZPSxFeHBpcnk6MjAxNi0wMi0xOX" +
                "0=");

                var rabbitMqHost = ConfigurationManager.AppSettings["RabbitMQHost"];
                if (string.IsNullOrEmpty(rabbitMqHost)) throw new Exception("You must provide a 'RabbitMQHost' app setting.");
                
                return new RabbitMqServer(rabbitMqHost)
                {
                    ErrorHandler = exception =>
                    {
                        Logger.For<RabbitMqServer>().Error("There was an error processing a message.", exception);
                    }
                };
            });
            container.RegisterSingleton<ICommandBus, CommandBus>();
            container.RegisterSingleton<IEventBus, EventBus>();
            container.RegisterSingleton<IBusLifetime, BusLifetime>();
        }

        public int Order
        {
            get { return 0; }
        }
    }
}