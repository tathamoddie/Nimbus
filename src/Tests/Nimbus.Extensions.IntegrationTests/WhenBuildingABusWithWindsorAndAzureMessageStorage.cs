using System;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Nimbus.Configuration;
using Nimbus.Infrastructure;
using Nimbus.IntegrationTests;
using Nimbus.LargeMessages.Azure.Configuration;
using Nimbus.LargeMessages.Azure.Infrastructure;
using Nimbus.Logger;
using Nimbus.Windsor.Configuration;
using NUnit.Framework;

namespace Nimbus.Extensions.IntegrationTests
{
    [TestFixture]
    public class WhenBuildingABusWithWindsorAndAzureMessageStorage
    {
        [Test]
        public void NothingShouldGoBang()
        {
            using (var container = new WindsorContainer())
            {
                var typeProvider = new AssemblyScanningTypeProvider();

                container.Register(Component.For<ILogger>()
                                            .ImplementedBy<ConsoleLogger>()
                                            .LifestyleSingleton());

                container.RegisterNimbus(typeProvider);

                container.Register(Component.For<ILargeMessageBodyStore>()
                                            .UsingFactoryMethod(c => new BlobStorageBuilder()
                                                                    .Configure()
                                                                    .UsingStorageAccountConnectionString(CommonResources.BlobStorageConnectionString)
                                                                    .WithLogger(c.Resolve<ILogger>())
                                                                    .Build())
                                            .LifestyleSingleton());

                container.Register(Component.For<IBus>()
                                            .UsingFactoryMethod(c => new BusBuilder().Configure()
                                                                                     .WithNames("IntegrationTestHarness", Environment.MachineName)
                                                                                     .WithConnectionString(
                                                                                         @"Endpoint=sb://shouldnotexist.example.com/;SharedAccessKeyName=IntegrationTestHarness;SharedAccessKey=borkborkbork=")
                                                                                     .WithLargeMessageStorage(
                                                                                         sc => sc.WithLargeMessageBodyStore(c.Resolve<ILargeMessageBodyStore>())
                                                                                                 .WithMaxSmallMessageSize(50*1024)
                                                                                                 .WithMaxLargeMessageSize(1024*1024))
                                                                                     .WithTypesFrom(typeProvider)
                                                                                     .WithDefaultTimeout(TimeSpan.FromSeconds(10))
                                                                                     .WithLogger(c.Resolve<ILogger>())
                                                                                     .Build())
                                            .LifestyleSingleton());

                container.Resolve<IBus>();
            }
        }
    }
}