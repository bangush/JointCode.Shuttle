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
    /// ���� ShuttleDomain ��Զ�̷�����ù���
    /// </summary>
    class ShuttleDomainFunctionalTestRunner : ShuttleTestRunner
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

            // ���� ShuttleDomain
            _serviceEnd1.CreateShuttleDomain();
            _serviceEnd2.CreateShuttleDomain();

            var key = Guid.NewGuid().ToString();
            _shuttleDomain = ShuttleDomainHelper.Create(key, key);

            // ע�����
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
            Console.WriteLine("׼�����й��ܲ���...");
            Console.WriteLine("=========================================================================");
            Console.WriteLine("���ǽ���ʾ��δ�Ĭ�� AppDomain �з��� ServiceEndDomain1��"
                + "�� ServiceEndDomain1 �з��� ServiceEndDomain2��"
                + "�� ServiceEndDomain2 �з���Ĭ�� AppDomain��");
            Console.WriteLine();

            _serviceEnd1.ConsumeServices();
            _serviceEnd2.ConsumeServices();

            _shuttleDomain.TryGetService(out _shuttleFunctionTest);

            ShuttleDomain_CallSimpleMethod();
            ShuttleDomain_SendAndReturnSimpleValue();
            ShuttleDomain_SendAndReturnSimpleValue2();
            ShuttleDomain_SendAndReturnSimpleValue3();

            ShuttleDomain_ReturnByBin();
            ShuttleDomain_SendAndReturnByBin();

            ShuttleDomain_SendAndReturnByVal();
            ShuttleDomain_SendAndReturnByRef();
            ShuttleDomain_TestAllFunctions();

            TryConsumeServicesAfterTheyAreUnregistered();
        }

        void ShuttleDomain_SendAndReturnByRef()
        {
            ShuttleDomain_SendAndReturnByRef(_shuttleFunctionTest);
        }

        void ShuttleDomain_TestAllFunctions()
        {
            ShuttleDomain_TestAllFunctions(_shuttleFunctionTest);
        }

        void ShuttleDomain_SendAndReturnByRef(IServiceFunctionTest test)
        {
            ICommonService service1;
            ICommonService service2;
            _shuttleDomain.TryGetService(out service1);
            _shuttleDomain.TryGetService(out service2);
            ICommonService service;
            test.SendAndReturnByRef("Liu", 100, service1, ref service2, out service);

#if DOASSERT
            var age2 = service2.Age;
            var name2 = service2.Name;
            var nameLength2 = service2.NameLength;
            var firstName2 = service2.FirstName;
            var lastName2 = service2.LastName;

            var age3 = service.Age;
            var name3 = service.Name;
            var nameLength3 = service.NameLength;
            var firstName3 = service.FirstName;
            var lastName3 = service.LastName;

            if (age2 != 140 || name2 != "LiuJingyi" || nameLength2 != 5 || firstName2 != "0" || lastName2 != "4"
                || age3 != 180 || name3 != "<Jingyi>Jingyi" || nameLength3 != 5 || firstName3 != "0" || lastName3 != "4")
                throw new AssertionException();
#endif
            #region �����ʵ��
            //ICommonService DoCreateOutCommonService(string s1, int i1, ICommonService serice)
            //{
            //    var result = new CommonService(i1 + serice.Age, s1 + serice.Name);
            //    var length = serice.NameLength;

            //    // Names ����ԭ��������֮�⣬�ټ��� 0-4 �� 5 ������
            //    for (int i = 0; i < length; i++)
            //        result.AddName(serice.GetName(i));
            //    for (int i = length; i < length + 5; i++)
            //        result.AddName((i - length).ToString());

            //    return result;
            //}

            //ICommonService DoCreateRefCommonService(string s1, int i1, ICommonService serice)
            //{
            //    var result = new CommonService(i1 + serice.Age, s1 + serice.Name);
            //    var length = serice.NameLength;

            //    // Names ����ԭ��������֮�⣬�ټ��� 0-4 �� 5 ������
            //    for (int i = 0; i < length; i++)
            //        result.AddName(serice.GetName(i));
            //    for (int i = length; i < length + 5; i++)
            //        result.AddName((i - length).ToString());

            //    return result;
            //}

            //public void SendAndReturnByRef(string s1, int i1, ICommonService serice1, ref ICommonService serice2, out ICommonService serice3)
            //{
            //    serice2 = DoCreateRefCommonService(s1, i1, serice1);
            //    serice3 = DoCreateOutCommonService("<" + serice1.Name + ">", i1 + serice1.Age, serice1);
            //} 
            #endregion
        }

        void ShuttleDomain_TestAllFunctions(IServiceFunctionTest test)
        {
            string s3, s2 = "s2";
            Version v3, v2 = new Version(1, 1, 1, 1), v1 = new Version(2, 2, 2, 2);
            int i3, i2 = 2;

            var testData = new TestData { Message = "Jingyi", Number = 40 };
            CommonData commonData = new CommonData { Code = 100, Country = "China", TestData = testData }, commonData2;

            ICommonService service3;
            ICommonService service4;
            ICommonService service;
            _shuttleDomain.TryGetService(out service3);
            _shuttleDomain.TryGetService(out service4);

            var result = test.TestAllFunctions("s1", ref s2, out s3,
                v1, ref v2, out v3,
                1, ref i2, out i3,
                commonData, out commonData2,
                service3, ref service4, out service);

#if DOASSERT
            var age2 = service4.Age;
            var name2 = service4.Name;
            var nameLength2 = service4.NameLength;
            var firstName2 = service4.FirstName;
            var lastName2 = service4.LastName;

            var age3 = service.Age;
            var name3 = service.Name;
            var nameLength3 = service.NameLength;
            var firstName3 = service.FirstName;
            var lastName3 = service.LastName;

            if (s2 != "[s1s2]" || s3 != "<s1>" || i2 != 3 || i3 != 101
                || v2.Major != 3 || v2.Minor != 3
                || v3.Major != 3 || v3.Minor != 3 || v3.Build != 3 || v3.Revision != 3
                || commonData2.Code != 200 || commonData2.Country != "[China]"
                || commonData2.TestData.Message != "[Jingyi]" || commonData2.TestData.Number != 140
                || age2 != 41 || name2 != "s1Jingyi" || nameLength2 != 5 || firstName2 != "0" || lastName2 != "4"
                || age3 != 81 || name3 != "[Jingyi]Jingyi" || nameLength3 != 5 || firstName3 != "0" || lastName3 != "4"
                || result.EntityId != 1500 || result.EntityName != "Fake")
                throw new AssertionException();
#endif
        }

        void TryConsumeServicesAfterTheyAreUnregistered()
        {
            Console.WriteLine();
            Console.WriteLine("׼��ע�� ServiceEndDomain1 �� ServiceEndDomain2 �ķ���...");
            Console.WriteLine("=========================================================================");
            Console.WriteLine("��ע�� ServiceEndDomain1 �� ServiceEndDomain2 �ķ���֮��"
                + "���޷��ٴ�Ĭ�� AppDomain �з��� ServiceEndDomain1 �� ServiceEndDomain2��");

            IServiceFunctionTest shuttleFunctionTest;
            ICommonService commonService;
            IFakeService fakeService;
            _serviceEnd1.DisposeShuttleDomain();
            _serviceEnd2.DisposeShuttleDomain();

            if (_shuttleDomain.TryGetService(out shuttleFunctionTest)
                || _shuttleDomain.TryGetService(out fakeService)
                || _shuttleDomain.TryGetService(out commonService))
                Console.WriteLine("����ע��ʧ�ܣ���Ȼ�ܹ����ʵ����� AppDomain ����ע���ķ���");
            else
                Console.WriteLine("����ע���ɹ���");
            Console.WriteLine();
        }

        public override void Dispose()
        {
            _shuttleDomain.Dispose();
            AppDomain.Unload(_serviceEnd1Domain);
            AppDomain.Unload(_serviceEnd2Domain);
        }
    }

    class ShuttleDomainFunctionalTest : Test
    {
        internal ShuttleDomainFunctionalTest()
        {
            Name = "SD functionality";
            Description = "Test functionalities of SD";
        }

        internal override void Run()
        {
            var test = new ShuttleDomainFunctionalTestRunner();
            test.Setup();
            test.RunTest();
            test.Dispose();
        }
    }
}