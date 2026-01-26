// Mock WCF attributes for Roslyn testing

using System;

namespace System.ServiceModel;

[AttributeUsage(AttributeTargets.Interface)]
public class ServiceContractAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
public class OperationContractAttribute : Attribute { }
