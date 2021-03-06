﻿#region Imports

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Reflection;
using System.Security.Permissions;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Web.Script.Serialization;
using System.Xml;
using System.Text.RegularExpressions;

#endregion Imports

namespace IPBan
{
    public class IPBanServiceRunner : ServiceBase
    {
        private IPBanService service;

        protected override void OnStart(string[] args)
        {
            base.OnStart(args);
            service = CreateService();
            service.Start();
        }

        protected override void OnStop()
        {
            service.Stop();
            service = null;
            base.OnStop();
        }

        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            base.OnSessionChange(changeDescription);
        }

        protected override bool OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
            return base.OnPowerEvent(powerStatus);
        }

        protected virtual void Preshutdown()
        {
        }

        protected override void OnCustomCommand(int command)
        {
            // command is SERVICE_CONTROL_PRESHUTDOWN
            if (command == 0x0000000F)
            {
                Preshutdown();
            }
            else
            {
                base.OnCustomCommand(command);
            }
        }

        public IPBanServiceRunner()
        {
            CanShutdown = false;
            CanStop = CanHandleSessionChangeEvent = CanHandlePowerEvent = true;
            var acceptedCommandsField = typeof(ServiceBase).GetField("acceptedCommands", BindingFlags.Instance | BindingFlags.NonPublic);
            if (acceptedCommandsField != null)
            {
                int acceptedCommands = (int)acceptedCommandsField.GetValue(this);
                acceptedCommands |= 0x00000100; // SERVICE_ACCEPT_PRESHUTDOWN;
                acceptedCommandsField.SetValue(this, acceptedCommands);
            }
        }

        public static IPBanService CreateService()
        {
            System.Type[] types = System.Reflection.Assembly.GetEntryAssembly().GetTypes();
            Type instanceType = typeof(IPBanService);
            foreach (Type type in types)
            {
                if (type.IsSubclassOf(instanceType))
                {
                    instanceType = type;
                    break;
                }
            }
            return (IPBanService)Activator.CreateInstance(instanceType, string.Empty);
        }

        public static int RunService(string[] args)
        {
            System.ServiceProcess.ServiceBase[] ServicesToRun;
            ServicesToRun = new System.ServiceProcess.ServiceBase[] { new IPBanServiceRunner() };
            System.ServiceProcess.ServiceBase.Run(ServicesToRun);
            return 0;
        }

        public static int RunConsole(string[] args)
        {
            IPBanService service = CreateService();
            if (args.Contains("test", StringComparer.OrdinalIgnoreCase))
            {
                service.RunTestsOnStart = true;
            }
            service.Start();
            Console.WriteLine("Press ENTER to quit");
            Console.ReadLine();
            service.Stop();
            return 0;
        }

        public static int Main(string[] args)
        {
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            if (Environment.UserInteractive)
            {
                return IPBanServiceRunner.RunConsole(args);
            }
            else
            {
                return IPBanServiceRunner.RunService(args);
            }
        }
    }
}
