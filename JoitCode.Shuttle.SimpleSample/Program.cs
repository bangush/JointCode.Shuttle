﻿using System;
using System.IO;
using System.Reflection;
using JointCode.Shuttle;
using JointCode.Shuttle.Services;
using JoitCode.Shuttle.SimpleContract;

namespace JoitCode.Shuttle.SimpleSample
{
    [ServiceInterface]
    public interface ISimpleService
    {
        string GetOutput(string input);
    }

    [ServiceClass(typeof(ISimpleService), Lifetime = LifetimeEnum.Transient)]
    public class SimpleService : ISimpleService
    {
        public string GetOutput(string input)
        {
            return string.Format
                ("SimpleService.GetOutput says: now, we are running in AppDomain: {0}, and the input passed from the caller is: {1}",
                    AppDomain.CurrentDomain.FriendlyName, input);
        }
    }

    public class ServiceEnd : MarshalByRefObject
    {
        // 这里必须使用一个字段来持有 ShuttleDomain 实例的引用，因为它是当前 AppDomain 与外部 AppDomain 之间通信的桥梁。
        // 如果该实例被垃圾回收，通过该实例注册的所有服务会被注销，且当前 AppDomain 与外部 AppDomain 之间将无法通信。
        // We need a field to keep the _shuttleDomain alive, because if it is garbage collected, we'll lose all communications
        // with other AppDomains.
        ShuttleDomain _shuttleDomain;

        public void RegisterServices()
        {
            // 注册服务组时，需要传递一个 Guid 对象
            // A Guid is needed when registering service group
            var guid = Guid.NewGuid();
            _shuttleDomain.RegisterServiceGroup(ref guid,
                new ServiceTypePair(typeof(ISimpleService), typeof(SimpleService)));
        }

        public void CreateShuttleDomain()
        {
            // 创建一个 ShuttleDomain
            // Create a ShuttleDomain object
            _shuttleDomain = ShuttleDomainHelper.Create("domain1", "domain1");
        }

        public void DisposeShuttleDomain()
        {
            _shuttleDomain.Dispose();
        }
    }

    class Program
    {
        const string SimpleServiceEndDll = "JoitCode.Shuttle.SimpleServiceEnd.dll";
        const string SimpleRemoteServiceEndType = "JoitCode.Shuttle.SimpleServiceEnd.SimpleRemoteServiceEnd2";

        static void Main(string[] args)
        {
            Console.WriteLine("Tests begin...");

            // 要使用 JointCode.Shuttle 进行跨 AppDomain 通信，首先必须初始化 ShuttleDomain。
            // 这个初始化操作一般在默认 AppDomain 执行，但也可以在其他 AppDomain 中执行，都是一样的。
            // To make cross-AppDomain communication with JointCode.Shuttle, initialize the ShuttleDomain at first.
            // It doesn't matter whether the initialization is done in default AppDomain or any other AppDomains, 
            // but it must be done before any ShuttleDomain instance is created.
            ShuttleDomain.Initialize();

            // 在默认 AppDomain 中，创建一个 ShuttleDomain。
            // 事实上，在需要与其他 AppDomain 进行通信的每个 AppDomain 中，都要有一个且只能有一个 ShuttleDomain 对象。
            // 尝试在一个 AppDomain 中创建多个 ShuttleDomain 对象时将会抛出异常。
            // 该对象用于与其他 AppDomain 中的 ShuttleDomain 对象通信。
            // Creating a ShuttleDomain instance in default AppDomain.
            // Actually, we needs one and only one ShuttleDomain instance in every AppDomain that needs to communicate 
            // with others. Trying to create another ShuttleDomain in the same AppDomain causes exceptions.
            // The ShuttleDomain instances communicates with each other across AppDomains.
            var str = Guid.NewGuid().ToString();
            var shuttleDomain = ShuttleDomainHelper.Create(str, str);

            if (CallServicesDefineInThisAssembly(shuttleDomain) 
                && CallServicesDefinedInAnotherAssembly(shuttleDomain))
            {
                Console.WriteLine("Tests completed...");
            }
            else
            {
                Console.WriteLine("Tests failed...");
            }

            shuttleDomain.Dispose();

            Console.Read();
        }

        static bool CallServicesDefineInThisAssembly(ShuttleDomain shuttleDomain)
        {
            Console.WriteLine();
            Console.WriteLine("=====================================");

            // 在默认 AppDomain 中创建一个子 AppDomain。
            // Creating a child AppDomain in default AppDomain.
            var serviceEnd1Domain = AppDomain.CreateDomain("ServiceEndDomain1", null, null);

            // 创建一个 ServiceEnd 对象以用于操作该子 AppDomain。
            // Creating a ServiceEnd instance for operating that child AppDomain.
            var serviceEnd = (ServiceEnd)serviceEnd1Domain.CreateInstanceAndUnwrap
                (typeof(Program).Assembly.FullName, "JoitCode.Shuttle.SimpleSample.ServiceEnd");

            // 在子 AppDomain 中，创建一个 ShuttleDomain 实例。
            // Creating a ShuttleDomain instance in the child AppDomain.
            serviceEnd.CreateShuttleDomain();

            // 在子 AppDomain 中，注册 ISimpleService 服务。
            // Registering ISimpleService service in the child AppDomain.
            serviceEnd.RegisterServices();


            // 在默认 AppDomain 中，获取子 AppDomain 中注册的 ISimpleService 服务实例。
            // 目前服务实例的默认生存期为 1 分钟。每次调用服务方法时，服务实例的生存期延长 30 秒。
            // Get the ISimpleService service in default AppDomain, which is registered by the child AppDomain.
            // The lifetime of service is default to 1 minute, every call to the service method extends that time for 30 seconds.
            ISimpleService service;
            if (shuttleDomain.TryGetService(out service))
            {
                try
                {
                    Console.WriteLine("Currently, we are running in AppDomain {0}, " +
                        "and we are trying to call a remote serivce that defined in the same library...",
                        AppDomain.CurrentDomain.FriendlyName);

                    Console.WriteLine();
                    // 调用子 AppDomain 中注册的 ISimpleService 服务实例的服务方法。
                    // Call the service method of ISimpleService service.
                    var output = service.GetOutput("Bingo");
                    Console.WriteLine(output);

                    Console.WriteLine();
                }
                catch
                {
                    Console.WriteLine();
                    Console.WriteLine("Failed to invoke the remote service method...");
                    return false;
                }
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("Failed to create remote service instance...");
                return false;
            }

            // 通知子 AppDomain 立即释放 ISimpleService 服务实例，而不用等待其生存期结束。
            // 此为可选操作，因为即使不手动释放 ISimpleService 服务实例，在其生命期结束之时系统也会自动释放该实例
            //（如果 ISimpleService 实现了 IDisposable，还会调用其 Dispose 方法）
            // Indicating the child AppDomain to release the ISimpleService service immediately, instead of waiting for its lifetime to end.
            // This is optional, because even if we don't do this explicitly, the ISimpleService service will still get released in the 
            // child AppDomain automatically when its lifetime ends.
            // And, if the ISimpleService derives from IDisposable, the Dispose method will also get called at that time.
            shuttleDomain.ReleaseService(service);

            // 在子 AppDomain 中，释放缓存的 ShuttleDomain 实例。这将会注销通过该实例注册的所有服务（在本示例中，即 ISimpleService 服务），
            // 并切断该 AppDomain 与所有 AppDomain 的通信。
            // Releasing the ShuttleDomain instance in the child AppDomain, this will unregister all services registered by that 
            // instance, and shut down all communications between that child AppDomain and all other AppDomains.
            serviceEnd.DisposeShuttleDomain();

            return true;
        }

        static bool CallServicesDefinedInAnotherAssembly(ShuttleDomain shuttleDomain)
        {
            Console.WriteLine();
            Console.WriteLine("=====================================");

            var remoteDomain = AppDomain.CreateDomain(Guid.NewGuid().ToString(), null, null);

            var currentDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var simpleServiceEndPath = Path.Combine(currentDir, SimpleServiceEndDll);
            var asmName = AssemblyName.GetAssemblyName(simpleServiceEndPath);
            var simpleRemoteServiceEnd = (SimpleRemoteServiceEnd)remoteDomain.CreateInstanceAndUnwrap
                (asmName.FullName, SimpleRemoteServiceEndType);

            simpleRemoteServiceEnd.CreateShuttleDomain();
            simpleRemoteServiceEnd.RegisterServices();

            ISimpleService2 service2;
            if (shuttleDomain.TryGetService(out service2))
            {
                try
                {
                    Console.WriteLine("Trying to call a remote serivce that defined in another library from AppDomain {0}...",
                        AppDomain.CurrentDomain.FriendlyName);

                    Console.WriteLine();
                    // 调用子 AppDomain 中注册的 ISimpleService2 服务实例的服务方法。
                    // Call the service method of ISimpleService2 service.
                    var output = service2.GetOutput("Duang");
                    Console.WriteLine(output);

                    Console.WriteLine();
                }
                catch
                {
                    Console.WriteLine();
                    Console.WriteLine("Failed to invoke the remote service method...");
                    return false;
                }
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("Failed to create remote service instance...");
                return false;
            }

            simpleRemoteServiceEnd.DisposeShuttleDomain();
            return true;
        }
    }
}
