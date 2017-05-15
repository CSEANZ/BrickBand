using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Autofac;

namespace BrickBand.UWP.Glue
{
    public class ProjectGlue
    {
        public IContainer Container { get; private set; }

        public void Init()
        {
            var builder = new ContainerBuilder();
            builder.RegisterAssemblyTypes(typeof(ProjectGlue).GetTypeInfo().Assembly)
                .Where(t => t.Name.EndsWith("Service", StringComparison.Ordinal))
                .AsSelf()
                .AsImplementedInterfaces();

            Container = builder.Build();
        }
    }
}
