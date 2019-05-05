using Convey.MessageBrokers.RabbitMQ;
using Convey.Tracing.Jaeger.RabbitMQ.Middlewares;
using Microsoft.Extensions.DependencyInjection;
using OpenTracing;

namespace Convey.Tracing.Jaeger.RabbitMQ
{
    public static class Extensions
    {
        public static IRabbitMqPluginRegister RegisterJaeger(this IRabbitMqPluginRegister register)
        {
            var tracer = register.ServiceProvider.GetService<ITracer>();
            register.AddPlugin(p => p.Register(pipe => pipe.Use<JaegerStagedMiddleware>(tracer)),
                ioc => ioc.AddSingleton(tracer));
            return register;
        }
    }
}