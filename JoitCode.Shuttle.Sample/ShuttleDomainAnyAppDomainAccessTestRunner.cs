#define DOASSERT

using System;
using System.IO;
using System.Reflection;
using JointCode.Common;
using JointCode.Shuttle.Services;
using JoitCode.Shuttle.Sample.Contract;

namespace JoitCode.Shuttle.Sample
{
    /// <summary>
    /// ���� ShuttleDomain �������� AppDomain �Ĺ���
    /// </summary>
    class ShuttleDomainAnyAppDomainAccessTestRunner : ShuttleTestRunner
    {
        AppDomain _serviceEnd1Domain, _serviceEnd2Domain;
        RemoteServiceEnd _serviceEnd1, _serviceEnd2;

        void Initialize()
        {
            var currentDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            _serviceEnd1Domain = AppDomain.CreateDomain("ServiceEndDomain1", null, null);
            var serviceEnd1Asm = AssemblyName.GetAssemblyName(Path.Combine(currentDir, ServiceEnd1Dll));
            _serviceEnd1 = (RemoteServiceEnd)_serviceEnd1Domain.CreateInstanceAndUnwrap
                (serviceEnd1Asm.FullName, ServiceEnd1Type);

            _serviceEnd2Domain = AppDomain.CreateDomain("ServiceEndDomain2", null, null);
            var serviceEnd2Asm = AssemblyName.GetAssemblyName(Path.Combine(currentDir, ServiceEnd2Dll));
            _serviceEnd2 = (RemoteServiceEnd)_serviceEnd2Domain.CreateInstanceAndUnwrap
                (serviceEnd2Asm.FullName, ServiceEnd2Type);

            // �ֱ��� 3 �� AppDomain �д��� ShuttleDomain
            _serviceEnd1.CreateShuttleDomain();
            _serviceEnd2.CreateShuttleDomain();
            var key = Guid.NewGuid().ToString();
            _shuttleDomain = ShuttleDomainHelper.Create(key, key);

            // �ֱ��� 3 �� AppDomain ��ע�����
            RegisterServices();
            _serviceEnd1.RegisterServices();
            _serviceEnd2.RegisterServices();
        }

        void RegisterServices()
        {
            var guid = Guid.NewGuid();
            _shuttleDomain.RegisterServiceGroup(ref guid,
                new ServiceTypePair(typeof(ISimpleService), typeof(SimpleService)));
        }

        public override bool Setup()
        {
            Initialize();
            return true;
        }

        public override void RunTest()
        {
            Console.WriteLine();
            Console.WriteLine("׼���������� AppDomain ���ʹ��ܲ���...");
            Console.WriteLine("=========================================================================");
            Console.WriteLine("���ǽ���ʾ��δ�Ĭ�� AppDomain �з��� ServiceEndDomain1��"
                + "�� ServiceEndDomain1 �з��� ServiceEndDomain2��"
                + "�� ServiceEndDomain2 �з���Ĭ�� AppDomain��");
            Console.WriteLine();

            // �� ServiceEndDomain1 �з��� ServiceEndDomain2
            _serviceEnd1.ConsumeServices();

            // �� ServiceEndDomain2 �з���Ĭ�� AppDomain
            _serviceEnd2.ConsumeServices();

            // ��Ĭ�� AppDomain �з��� ServiceEndDomain1
            _shuttleDomain.TryGetService(out _shuttleFunctionTest);
            Console.WriteLine("AppDomain [{0}], before calling the remote service: ", AppDomain.CurrentDomain.FriendlyName);
            _shuttleFunctionTest.PrintMessage();
            Console.WriteLine("AppDomain [{0}], after calling the remote service!", AppDomain.CurrentDomain.FriendlyName);
        }

        public override void Dispose()
        {
            _shuttleDomain.Dispose();
            AppDomain.Unload(_serviceEnd1Domain);
            AppDomain.Unload(_serviceEnd2Domain);
        }
    }

    class ShuttleDomainAnyAppDomainAccessTest : Test
    {
        internal ShuttleDomainAnyAppDomainAccessTest()
        {
            Name = "SD any AppDomain commu";
            Description = "Use SD to access any AppDomain";
        }

        internal override void Run()
        {
            var test = new ShuttleDomainAnyAppDomainAccessTestRunner();
            test.Setup();
            test.RunTest();
            test.Dispose();
        }
    }
}