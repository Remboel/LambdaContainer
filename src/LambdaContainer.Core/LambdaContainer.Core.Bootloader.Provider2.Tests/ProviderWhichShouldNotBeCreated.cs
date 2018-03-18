using System;
using LambdaContainer.Core.Contracts;

namespace LambdaContainer.Core.BootTests.Provider2.Tests
{
    public class ProviderWhichShouldNotBeCreated : ILambdaContainerRegistry
    {
        public ProviderWhichShouldNotBeCreated(int doh)
        {
        }

        public void WriteContentsTo(ILambdaContainerRegistrationsRecorder recorder)
        {
            throw new NotSupportedException("I should not be called due to my non- empty ctor");
        }
    }
}
