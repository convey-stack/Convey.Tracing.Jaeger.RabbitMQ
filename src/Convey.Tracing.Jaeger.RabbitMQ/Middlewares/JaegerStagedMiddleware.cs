using System;
using System.Threading;
using System.Threading.Tasks;
using Convey.MessageBrokers;
using Jaeger;
using OpenTracing;
using OpenTracing.Tag;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace Convey.Tracing.Jaeger.RabbitMQ.Middlewares
{
    internal sealed class JaegerStagedMiddleware : StagedMiddleware
    {
        private readonly ITracer _tracer;

        public JaegerStagedMiddleware(ITracer tracer)
            => _tracer = tracer;

        public override string StageMarker => RawRabbit.Pipe.StageMarker.MessageDeserialized;

        public override async Task InvokeAsync(IPipeContext context, CancellationToken token = new CancellationToken())
        {
            var correlationContext = (ICorrelationContext) context.GetMessageContext();
            var message = context.GetMessageType().Name.Underscore().ToLowerInvariant();

            using (var scope = BuildScope(message, correlationContext.SpanContext))
            {
                var span = scope.Span;
                span.Log($"Started processing: {message}");
                try
                {
                    await Next.InvokeAsync(context, token);
                }
                catch (Exception ex)
                {
                    span.SetTag(Tags.Error, true);
                    span.Log(ex.Message);
                }

                span.Log($"Finished processing: {message}");
            }

            await Next.InvokeAsync(context, token);
        }

        private IScope BuildScope(string message, string serializedSpanContext)
        {
            var spanBuilder = _tracer
                .BuildSpan($"processing-{message}")
                .WithTag("message-type", message);

            if (string.IsNullOrEmpty(serializedSpanContext))
            {
                return spanBuilder.StartActive(true);
            }

            var spanContext = SpanContext.ContextFromString(serializedSpanContext);

            return spanBuilder
                .AddReference(References.FollowsFrom, spanContext)
                .StartActive(true);
        }
    }
}