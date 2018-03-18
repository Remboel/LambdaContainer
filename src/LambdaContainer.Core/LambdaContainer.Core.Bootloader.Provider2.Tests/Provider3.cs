using System;
using LambdaContainer.Core.Contracts;
using LambdaContainer.Core.Setup;

namespace LambdaContainer.Core.BootTests.Provider2.Tests
{
    public interface IAclass
    {
        IAchild ChildByInterface { get; set; }
        AChild ChildAutoInjected { get; set; }
    }
    public class AClass :IAclass
    {
        public IAchild ChildByInterface { get; set; }
        public AChild ChildAutoInjected { get; set; }

        public AClass(IAchild childByInterface, AChild childAutoInjected)
        {
            ChildByInterface = childByInterface;
            ChildAutoInjected = childAutoInjected;
        }
    }

    public interface IAchild { }
    public interface IAchild2 { }
    public class AChild : IAchild, IAchild2
    {
        
    }

    public class PropertyInjectionTestType
    {
        private IAclass _aClass;

        [LambdaContainerInjection]
        public IAchild InjectedChild1 { get; set; }

        [LambdaContainerInjection]
        public IAchild2 InjectedChild2 { get; set; }

        [LambdaContainerInjection]
        public void Inject(IAclass aClass)
        {
            _aClass = aClass;
        }

        public IAclass GetInjectedClass()
        {
            return _aClass;

        }
    }

    public class MethodInjectionTestType
    {
        private PropertyInjectionTestType _v;

        [LambdaContainerInjection]
        public void Inject(PropertyInjectionTestType v)
        {
            _v = v;
        }

        public PropertyInjectionTestType GetInjectedTypeThatHadPropertyInjection()
        {
            return _v;

        }
    }

    public interface IClassVariant{}
    public class ClassVariant1 : IClassVariant{ }
    public class ClassVariant2 : IClassVariant{ }

    public class ContructorInjectAllOfType
    {
        public IClassVariant[] Variants { get; set; }

        public ContructorInjectAllOfType(IClassVariant[] variants)
        {
            Variants = variants;
        }
    }

    public class Provider3 : ILambdaContainerRegistry
    {
        public void Read(ILambdaContainerRegistrationsBuilder<ITypeMappingRegistrations> builder)
        {
            builder.Build()
                .Register<IAchild, AChild>()
                .Register<IAclass, AClass>()
                .RegisterByName<IClassVariant,ClassVariant1>("one")
                .RegisterByName<IClassVariant,ClassVariant2>("two");
            builder.WithOutputLifetime(OutputLifetime.Singleton).Build()
                .Register<IAchild2, AChild>();

        }

        public void WriteContentsTo(ILambdaContainerRegistrationsRecorder recorder)
        {
            recorder.Record<ITypeMappingRegistrations>(Read);
        }
    }
}
