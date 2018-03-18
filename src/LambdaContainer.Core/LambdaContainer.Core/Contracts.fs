// The MIT License(MIT)
// Copyright(c) 2017 Morten Rembøl Jacobsen

// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and 
// associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
// and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE 
// WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR 
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, 
// ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

//NOTE: On the boundary contract level, we will be nice prevent usage of F#-only types.
namespace LambdaContainer.Core.Contracts
open System
open System.Collections.Generic
open LambdaContainer.Core.NetFrameworkEx

exception OverlappingRegistrationException of string
exception IncompatibleRegistrationException of string
exception InvalidRegistrationException of string
exception InvalidRegistrationsException of string
exception MissingRegistrationException of string
exception FactoryInvocationException of string * exn
exception CyclicDependencyException of string

///<summary>Use to mark which ctor to be used when auto constructing instances based on type mappings. If not used, LambdaContainer will select the public ctor with the most arguments.</summary>
[<Sealed>]
[<AttributeUsageAttribute(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)>]
type LambdaContainerInjectionConstructorAttribute() =
    class 
        inherit Attribute()
    end

///<summary>Use to mark a public method and/or property, which should have it's arguments injected during type buildup(used when auto constructing instances based on type mappings).</summary>
[<Sealed>]
[<AttributeUsageAttribute(AttributeTargets.Method ||| AttributeTargets.Property, AllowMultiple = false, Inherited = false)>]
type LambdaContainerInjectionAttribute() =
    class 
        inherit Attribute()
    end

type public OutputLifetime =
    | Transient = 0
    | ThreadSingleton = 1
    | Singleton = 2

type public DisposalScope =
    | Container = 0
    | SubScope = 1

type public ILambdaContainerRegistrations = interface end

type public ILambdaContainerRegistrationsBuilder<'registrationsType when 'registrationsType :> ILambdaContainerRegistrations> =
    abstract member WithOutputLifetime : lifetime : OutputLifetime -> ILambdaContainerRegistrationsBuilder<'registrationsType>
    abstract member WithDisposalScope : scope : DisposalScope -> ILambdaContainerRegistrationsBuilder<'registrationsType>
    abstract member Build : unit -> 'registrationsType

type public LambdaRegistrationsCommand<'registrationsType when 'registrationsType :> ILambdaContainerRegistrations> = 
    delegate of builder : ILambdaContainerRegistrationsBuilder<'registrationsType> -> unit

type public ILambdaContainerRegistrationsRecorder =
    abstract member Record<'registrationsType when 'registrationsType :> ILambdaContainerRegistrations> : registrations : LambdaRegistrationsCommand<'registrationsType> -> ILambdaContainerRegistrationsRecorder

type public IResolutionScope =
    abstract member GetInstanceOfType : instanceType : Type -> Object
    abstract member GetInstanceOfTypeOrNull : instanceType : Type -> Object
    abstract member GetInstanceOfTypeByName : instanceType : Type -> name : string-> Object
    abstract member GetInstanceOfTypeByNameOrNull : instanceType : Type -> name : string-> Object
    abstract member IsTypeRegistered : instanceType : Type -> bool
    abstract member IsTypeRegisteredByName : instanceType : Type ->  name : string -> bool
    abstract member GetAllInstancesOfType : instanceType : Type -> Object seq
    abstract member GetInstance<'a> : unit -> 'a
    abstract member GetInstanceOrNull<'a when 'a:null> : unit -> 'a
    abstract member GetInstanceByName<'a> : name : string -> 'a
    abstract member GetInstanceByNameOrNull<'a when 'a:null> : name : string -> 'a
    abstract member GetAllInstances<'a> : unit -> 'a seq
    abstract member IsRegistered<'a> : unit -> bool
    abstract member IsRegisteredByName<'a> : name : string -> bool

type public ILambdaContainer =
    inherit IDisposable
    inherit ILambdaContainerScoping
    inherit IResolutionScope

and public ILambdaContainerScoping =
    abstract member CreateSubScope : unit -> ILambdaContainer
    abstract member CreateSubScopeWith : recordAdditionalRegistrations : Action<ILambdaContainerRegistrationsRecorder> -> ILambdaContainer
    abstract member WithCustomizedResolution<'registrationsType when 'registrationsType :> ILambdaContainerRegistrations> : applyTransientsRegistrations : Action<'registrationsType> -> IResolutionScope

type public IFactoryRegistrations =
    inherit ILambdaContainerRegistrations
    abstract member Register<'a> : factory : Func<ILambdaContainer,'a> -> IFactoryRegistrations
    abstract member RegisterByName<'a> : factory : Func<ILambdaContainer,'a> * name : string -> IFactoryRegistrations

type public ITypeMappingRegistrations =
    inherit ILambdaContainerRegistrations
    abstract member Register<'a,'b> : unit -> ITypeMappingRegistrations
    abstract member RegisterByName<'a,'b> : name : string -> ITypeMappingRegistrations

type public ICoreRegistrations =
    inherit ILambdaContainerRegistrations
    abstract member RegisterFactory : outputType : Type -> factory : Func<ILambdaContainer,Object> -> ICoreRegistrations
    abstract member RegisterFactoryByName : outputType : Type -> factory : Func<ILambdaContainer,Object> -> name : string -> ICoreRegistrations
    abstract member RegisterMapping : outputType : Type -> implementation : Type -> ICoreRegistrations
    abstract member RegisterMappingByName : outputType : Type -> implementation : Type -> name : string -> ICoreRegistrations

type public ILambdaContainerRegistry = 
    abstract member WriteContentsTo : recorder : ILambdaContainerRegistrationsRecorder -> unit

//Type Convention registrations
type LoadTypes = Func<Type seq>
type MatchType = Predicate<Type>
type ComputeAbstractions = Func<Type,Type seq>

type NamingContext =
    {
        Abstraction : Type
        Implementation : Type
    }

type internal ProvideName = NamingContext -> string option
type internal RegistrationConvention =
    | Include of LoadTypes * RegistrationConvention
    | ScopeTo of MatchType * RegistrationConvention
    | Remove of MatchType * RegistrationConvention
    | ApplyNamingStrategy of matchAbstraction : MatchType * ProvideName * RegistrationConvention
    | Register of ComputeAbstractions * RegistrationConvention
    | Complete

type public ITypeLoader =
    abstract member AssemblyTypes<'a> : unit -> LoadTypes
    abstract member AssemblyTypesFrom : System.Reflection.Assembly -> LoadTypes
    abstract member Types : Type seq -> LoadTypes
    abstract member Type<'a> : unit -> LoadTypes

type public ITypeCondition =
    abstract member TypeOf<'a> : unit -> MatchType
    abstract member TypeOf : Type -> MatchType
    abstract member ImplementationsOf<'a> : unit -> MatchType
    abstract member ImplementationsOf : Type -> MatchType
    abstract member Match : MatchType -> MatchType

type public ITypeAbstractionSelector =
    abstract member ImplementationsOf<'a> : unit -> ComputeAbstractions
    abstract member ImplementationsOf : Type -> ComputeAbstractions
    abstract member ImplementationsOfTypes : Type seq -> ComputeAbstractions
    abstract member ImplementedInterfaces : unit -> ComputeAbstractions
    abstract member ImplementedInterfacesFiltered : filter:  Predicate<Type> -> ComputeAbstractions

type public IConventionSpecification =
    abstract member Append : selectTypes : Func<ITypeLoader, LoadTypes> -> IConventionSpecification
    abstract member ScopeTo : selectType : Func<ITypeCondition, MatchType> -> IConventionSpecification
    abstract member Except : selectType : Func<ITypeCondition, MatchType> -> IConventionSpecification
    abstract member UsingNamingStrategy : pairUpAbstractionWithNaming : Func<ITypeCondition, MatchType> * Func<NamingContext, string> -> IConventionSpecification
    abstract member UsingUniqueNamingStrategy : pickAbstractions : Func<ITypeCondition, MatchType> -> IConventionSpecification
    abstract member ClearNamingStrategy : selectAbstraction : Func<ITypeCondition, MatchType> -> IConventionSpecification
    abstract member Register : selectAbstraction : Func<ITypeAbstractionSelector, ComputeAbstractions> -> IConventionSpecification

type public IRegistrationConvention =
    inherit ILambdaContainerRegistrations
    abstract member Register : Func<IConventionSpecification, IConventionSpecification> -> IRegistrationConvention

//Container configuration
type public RegistrationCollisionModes = 
    | Fail = 0 
    | Ignore = 1 
    | Override = 2

type public LambdaContainerAssemblyScannerConfiguration internal() =
    let mutable _registryScannerBaseDir = AppDomain.CurrentDomain.BaseDirectory
    let mutable _enabled = true
    let mutable _registryFileNameCondition = new Func<IO.FileInfo,bool>(fun _ -> true)

    member this.Enabled with get () = _enabled
    member this.Enabled with set (newVal) = _enabled <- newVal

    member this.RegistryScannerBaseDir with get () = _registryScannerBaseDir
    member this.RegistryScannerBaseDir with set (newVal) = 
                                                            ParameterGuard.checkForNull newVal
                                                            _registryScannerBaseDir <- newVal

    
    member this.RegistryFileNameCondition with get () = _registryFileNameCondition
    member this.RegistryFileNameCondition with set (newVal:Func<IO.FileInfo,bool>) = 
                                                            ParameterGuard.checkForNull newVal
                                                            _registryFileNameCondition <- newVal

type public LambdaContainerConfiguration internal() =
    let mutable _registrationCollisionMode = RegistrationCollisionModes.Fail

    member this.RegistrationCollisionMode with get () = _registrationCollisionMode
    member this.RegistrationCollisionMode with set (newVal) = _registrationCollisionMode <- newVal