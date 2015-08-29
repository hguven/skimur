﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Infrastructure.Messaging.Handling;
using Skimur;

namespace Infrastructure.Messaging
{
    public class Registrar : IRegistrar
    {
        public void Register(SimpleInjector.Container container)
        {
            container.RegisterSingleton<IEventDiscovery, EventDiscovery>();
            container.RegisterSingleton<ICommandDiscovery, CommandDiscovery>();
        }

        public int Order
        {
            get { return 0; }
        }
    }
}
