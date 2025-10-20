"""
OpenTelemetry configuration for the GraphRag API.
Compatible with .NET Aspire's automatic OTLP configuration.
"""
import logging
import os

from opentelemetry import trace, metrics
from opentelemetry.sdk.trace import TracerProvider
from opentelemetry.sdk.trace.export import BatchSpanProcessor, ConsoleSpanExporter
from opentelemetry.sdk.metrics import MeterProvider
from opentelemetry.sdk.metrics.export import PeriodicExportingMetricReader, ConsoleMetricExporter
from opentelemetry.sdk.resources import Resource, SERVICE_NAME, SERVICE_VERSION
from opentelemetry.instrumentation.logging import LoggingInstrumentor
from opentelemetry.exporter.otlp.proto.grpc.trace_exporter import OTLPSpanExporter
from opentelemetry.exporter.otlp.proto.grpc.metric_exporter import OTLPMetricExporter

logger = logging.getLogger(__name__)


def configure_opentelemetry():
    """
    Configure OpenTelemetry for tracing and metrics.
    Compatible with .NET Aspire's automatic OTLP configuration.

    .NET Aspire automatically sets these environment variables:
    - OTEL_EXPORTER_OTLP_ENDPOINT: The OTLP endpoint URL
    - OTEL_EXPORTER_OTLP_HEADERS: Authentication headers if needed
    - OTEL_SERVICE_NAME: Service name (optional, we set our own)

    Returns:
        tuple: (tracer, meter) - OpenTelemetry tracer and meter instances
    """
    # Get service name from environment or use default
    service_name = os.getenv("OTEL_SERVICE_NAME", "graphrag-api")

    # Service resource - compatible with Aspire's resource attributes
    resource = Resource(attributes={
        SERVICE_NAME: service_name,
        SERVICE_VERSION: "1.0.0",
        "environment": os.getenv("ENVIRONMENT", "development")
    })

    # Configure Tracing
    trace_provider = TracerProvider(resource=resource)

    # Check for OTLP endpoint (set by .NET Aspire or manually)
    otlp_endpoint = os.getenv("OTEL_EXPORTER_OTLP_ENDPOINT")
    otlp_headers = os.getenv("OTEL_EXPORTER_OTLP_HEADERS", "")

    if otlp_endpoint:
        # Parse headers if provided (format: key1=value1,key2=value2)
        headers_dict = {}
        if otlp_headers:
            for header in otlp_headers.split(","):
                if "=" in header:
                    key, value = header.split("=", 1)
                    headers_dict[key.strip()] = value.strip()

        # Configure OTLP exporter for Aspire
        otlp_span_exporter = OTLPSpanExporter(
            endpoint=otlp_endpoint,
            headers=headers_dict if headers_dict else None,
            insecure=os.getenv("OTEL_EXPORTER_OTLP_INSECURE", "false").lower() == "true"
        )
        trace_provider.add_span_processor(BatchSpanProcessor(otlp_span_exporter))
        logger.info(f"OTLP trace exporter configured for .NET Aspire endpoint: {otlp_endpoint}")
    else:
        # Fallback to console exporter for local development
        console_exporter = ConsoleSpanExporter()
        trace_provider.add_span_processor(BatchSpanProcessor(console_exporter))
        logger.info("Console trace exporter configured (standalone development mode)")

    trace.set_tracer_provider(trace_provider)

    # Configure Metrics
    if otlp_endpoint:
        headers_dict = {}
        if otlp_headers:
            for header in otlp_headers.split(","):
                if "=" in header:
                    key, value = header.split("=", 1)
                    headers_dict[key.strip()] = value.strip()

        metric_exporter = OTLPMetricExporter(
            endpoint=otlp_endpoint,
            headers=headers_dict if headers_dict else None,
            insecure=os.getenv("OTEL_EXPORTER_OTLP_INSECURE", "false").lower() == "true"
        )
        metric_reader = PeriodicExportingMetricReader(metric_exporter, export_interval_millis=5000)
        logger.info(f"OTLP metric exporter configured for .NET Aspire endpoint: {otlp_endpoint}")
    else:
        metric_exporter = ConsoleMetricExporter()
        metric_reader = PeriodicExportingMetricReader(metric_exporter, export_interval_millis=60000)

    meter_provider = MeterProvider(resource=resource, metric_readers=[metric_reader])
    metrics.set_meter_provider(meter_provider)

    # Instrument logging
    LoggingInstrumentor().instrument(set_logging_format=True)

    logger.info(f"OpenTelemetry configured successfully for service: {service_name}")
    logger.info(f"Running in {'Aspire' if otlp_endpoint else 'standalone'} mode")

    return trace.get_tracer(__name__), metrics.get_meter(__name__)
