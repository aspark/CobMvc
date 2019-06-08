using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Text;

//thx https://github.com/WebApiContrib/WebAPIContrib.Core/tree/master/src/WebApiContrib.Core.Formatter.Protobuf
namespace CobMvc.Protobuf
{
    public static class ProtobufMVCExtensions
    {
        public static IMvcBuilder AddProtobufFormatters(this IMvcBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return AddProtobufFormatters(builder, protobufFormatterOptionsConfiguration: null);
        }

        public static IMvcCoreBuilder AddProtobufFormatters(this IMvcCoreBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return AddProtobufFormatters(builder, protobufFormatterOptionsConfiguration: null);
        }

        public static IMvcBuilder AddProtobufFormatters(this IMvcBuilder builder, Action<ProtobufFormatterOptions> protobufFormatterOptionsConfiguration)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var protobufFormatterOptions = new ProtobufFormatterOptions();
            protobufFormatterOptionsConfiguration?.Invoke(protobufFormatterOptions);

            foreach (var extension in protobufFormatterOptions.SupportedExtensions)
            {
                foreach (var contentType in protobufFormatterOptions.SupportedContentTypes)
                {
                    builder.AddFormatterMappings(m => m.SetMediaTypeMappingForFormat(extension, new MediaTypeHeaderValue(contentType)));
                }
            }

            builder.AddMvcOptions(options => options.InputFormatters.Add(new ProtobufInputFormatter(protobufFormatterOptions)));
            builder.AddMvcOptions(options => options.OutputFormatters.Add(new ProtobufOutputFormatter(protobufFormatterOptions)));

            return builder;
        }

        public static IMvcCoreBuilder AddProtobufFormatters(this IMvcCoreBuilder builder, Action<ProtobufFormatterOptions> protobufFormatterOptionsConfiguration)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var protobufFormatterOptions = new ProtobufFormatterOptions();
            protobufFormatterOptionsConfiguration?.Invoke(protobufFormatterOptions);

            foreach (var extension in protobufFormatterOptions.SupportedExtensions)
            {
                foreach (var contentType in protobufFormatterOptions.SupportedContentTypes)
                {
                    builder.AddFormatterMappings(m => m.SetMediaTypeMappingForFormat(extension, new MediaTypeHeaderValue(contentType)));
                }
            }

            builder.AddMvcOptions(options => options.InputFormatters.Add(new ProtobufInputFormatter(protobufFormatterOptions)));
            builder.AddMvcOptions(options => options.OutputFormatters.Add(new ProtobufOutputFormatter(protobufFormatterOptions)));

            return builder;
        }
    }
}
