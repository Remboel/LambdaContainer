using LambdaContainer.Core.Contracts;

namespace LambdaContainer.Core.BootTests.Provider2.Tests
{
    public class Provider2 : ILambdaContainerRegistry
    {
        public void Read(ILambdaContainerRegistrationsBuilder<IFactoryRegistrations> builder)
        {
            var fullName = GetType().FullName;
            builder.Build().RegisterByName(container => fullName, fullName);   
        }

        public void WriteContentsTo(ILambdaContainerRegistrationsRecorder recorder)
        {
            recorder.Record<IFactoryRegistrations>(Read);
        }
    }
}
