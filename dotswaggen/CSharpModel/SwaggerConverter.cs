﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using dotswaggen.CSharpModel.DataTypes;
using dotswaggen.CSharpModel.Operations;
using dotswaggen.Interfaces;
using dotswaggen.Swagger;
using DotLiquid;
using Api = dotswaggen.CSharpModel.Operations.Api;
using DataType = dotswaggen.CSharpModel.DataTypes.DataType;
using Operation = dotswaggen.CSharpModel.Operations.Operation;
using Parameter = dotswaggen.CSharpModel.Operations.Parameter;
using SwaggerTypes = dotswaggen.Swagger.DataTypeRegistry;

namespace dotswaggen.CSharpModel
{
    public class SwaggerConverter : BaseSwaggerConverter
    {
        public static Dictionary<SwaggerTypes.CommonNames, string> SwaggerTypeMappings = new Dictionary
            <SwaggerTypes.CommonNames, string>
        {
            {SwaggerTypes.CommonNames.INTEGER, "int"},
            {SwaggerTypes.CommonNames.LONG, "long"},
            {SwaggerTypes.CommonNames.FLOAT, "float"},
            {SwaggerTypes.CommonNames.DOUBLE, "double"},
            {SwaggerTypes.CommonNames.STRING, "string"},
            {SwaggerTypes.CommonNames.BYTE, "byte"},
            {SwaggerTypes.CommonNames.BOOLEAN, "bool"},
            {SwaggerTypes.CommonNames.DATE, "DateTime"},
            {SwaggerTypes.CommonNames.DATETIME, "DateTime"}
        };

        public SwaggerConverter(ApiDeclaration root) : base(root)
        {
        }

        public override Api[] Apis
        {
            get
            {
                var apis = new List<Api>();

                foreach (var swApi in Root.Apis)
                {
                    var api = new Api
                    {
                        Description = swApi.Description,
                        Path = swApi.Path,
                        Operations = swApi.Operations.Select(swOp =>
                        {
                            var op = new Operation
                            {
                                Method = (HttpMethod) Enum.Parse(typeof (HttpMethod), swOp.Method, true),
                                Nickname = swOp.Nickname,
                                ReturnType = GetTypeString(swOp),
                                Description = swOp.Summary,
                                Parameters = swOp.Parameters.Select(swP => new Parameter
                                {
                                    Location =
                                        (ParameterType) Enum.Parse(typeof (ParameterType), swP.ParamType, true),
                                    Name = GetValidIdentifier(swP.Name),
                                    Type = GetTypeString(swP)
                                }).ToArray(),
                                Responses = swOp.ResponseMessages.Select(swResp => new Response
                                {
                                    Code = swResp.Code,
                                    Message = swResp.Message
                                }).ToArray()
                            };

                            return op;
                        }).ToArray()
                    };


                    apis.Add(api);
                }

                return apis.ToArray();
            }
        }

        public override IDataType[] Models
        {
            get
            {
                var models = new Dictionary<string, DataType>();
                Func<string, DataType> getModel = s =>
                {
                    DataType model;
                    if (!models.TryGetValue(s, out model))
                    {
                        model = new DataType();
                        models.Add(s, model);
                    }
                    return model;
                };

                foreach (var m in Root.Models)
                {
                    var model = getModel(m.Key);

                    model.Description = m.Value.Description;
                    model.Name = GetValidIdentifier(m.Key);
                    model.Properties = m.Value.Properties.Select(p =>
                        new DataProperty
                        {
                            Description = p.Value.Description,
                            Name = GetValidIdentifier(p.Key),
                            Type = GetTypeString(p.Value)
                        }
                        ).ToArray();

                    if (m.Value.SubTypes != null)
                        foreach (var subType in m.Value.SubTypes)
                        {
                            getModel(subType).ParentType = GetValidIdentifier(m.Key);
                        }
                }

                return models.Values.ToArray();
            }
        }

        public override string DefaultExtension
        {
            get { return "cs"; }
        }

        public static HashSet<string> ReservedIdentifiers
        {
            get
            {
                return new HashSet<string>(
                    new[]
                    {
                        "abstract",
                        "as",
                        "base",
                        "bool",
                        "break",
                        "byte",
                        "case",
                        "catch",
                        "char",
                        "checked",
                        "class",
                        "const",
                        "continue",
                        "decimal",
                        "default",
                        "delegate",
                        "do",
                        "double",
                        "else",
                        "enum",
                        "event",
                        "explicit",
                        "extern",
                        "false",
                        "finally",
                        "fixed",
                        "float",
                        "for",
                        "foreach",
                        "goto",
                        "if",
                        "implicit",
                        "in",
                        "in",
                        "int",
                        "interface",
                        "internal",
                        "is",
                        "lock",
                        "long",
                        "namespace",
                        "new",
                        "null",
                        "object",
                        "operator",
                        "out",
                        "out",
                        "override",
                        "params",
                        "private",
                        "protected",
                        "public",
                        "readonly",
                        "ref",
                        "return",
                        "sbyte",
                        "sealed",
                        "short",
                        "sizeof",
                        "stackalloc",
                        "static",
                        "string",
                        "struct",
                        "switch",
                        "this",
                        "throw",
                        "true",
                        "try",
                        "typeof",
                        "uint",
                        "ulong",
                        "unchecked",
                        "unsafe",
                        "ushort",
                        "using",
                        "virtual",
                        "void",
                        "volatile",
                        "while"
                    }
                    );
            }
        }

        public static string GetTypeString(TypedElement te)
        {
            var type = te.Type ?? te.Ref;
            switch (type)
            {
                case "array":
                    return string.Format("List<{0}>", GetTypeString(te.Items));
                default:
                {
                    SwaggerTypes.WithPrimitiveType(type, te.Format, tcn => type = SwaggerTypeMappings[tcn]);
                    return type;
                }
            }
        }

        public static string GetValidIdentifier(string maybeValidIdentifier)
        {
            maybeValidIdentifier = Regex.Replace(maybeValidIdentifier, "[^a-zA-Z_0-9]", "_");

            if (!Regex.IsMatch(maybeValidIdentifier, @"^[_a-zA-Z@]") ||
                ReservedIdentifiers.Contains(maybeValidIdentifier))
                maybeValidIdentifier = string.Concat("@", maybeValidIdentifier);

            return maybeValidIdentifier;
        }

        public override void RegisterSafeTypes()
        {
            //Set up enums required by the CSharp view of the world
            Template.RegisterSafeType(typeof (HttpMethod), o => o.ToString());
            Template.RegisterSafeType(typeof (ParameterType), o => o.ToString());
        }
    }
}