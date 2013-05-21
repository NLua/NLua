
EXTRA_DIST =  

# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize- -debug "-define:DEBUG"
ASSEMBLY = ../../Run/Debug/NLua.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES =  \
	../KeraLua/src/bin/Debug/KeraLua.dll \
	../KopiLua/Bin/Debug/KopiLua.dll
BUILD_DIR = ../../Run/Debug

NLUA_DLL_MDB_SOURCE=../../Run/Debug/NLua.dll.mdb
NLUA_DLL_MDB=$(BUILD_DIR)/NLua.dll.mdb
KERALUA_DLL_SOURCE=../KeraLua/src/bin/Debug/KeraLua.dll
KERALUA_DLL_MDB_SOURCE=../KeraLua/src/bin/Debug/KeraLua.dll.mdb
KERALUA_DLL_MDB=$(BUILD_DIR)/KeraLua.dll.mdb
KOPILUA_DLL_SOURCE=../KopiLua/Bin/Debug/KopiLua.dll
KOPILUA_DLL_MDB_SOURCE=../KopiLua/Bin/Debug/KopiLua.dll.mdb
KOPILUA_DLL_MDB=$(BUILD_DIR)/KopiLua.dll.mdb

endif

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ "-define:RELEASE"
ASSEMBLY = ../../Run/Release/NLua.dll
ASSEMBLY_MDB = 
COMPILE_TARGET = library
PROJECT_REFERENCES =  \
	../KeraLua/src/bin/Release/KeraLua.dll \
	../KopiLua/Bin/Release/KopiLua.dll
BUILD_DIR = ../../Run/Release

NLUA_DLL_MDB=
KERALUA_DLL_SOURCE=../KeraLua/src/bin/Release/KeraLua.dll
KERALUA_DLL_MDB=
KOPILUA_DLL_SOURCE=../KopiLua/Bin/Release/KopiLua.dll
KOPILUA_DLL_MDB=

endif

AL=al2
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES = \
	$(NLUA_DLL_MDB) \
	$(KERALUA_DLL) \
	$(KERALUA_DLL_MDB) \
	$(KOPILUA_DLL) \
	$(KOPILUA_DLL_MDB)  

LINUX_PKGCONFIG = \
	$(NLUA_PC)  


RESGEN=resgen2
	
all: $(ASSEMBLY) $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

FILES = \
	CheckType.cs \
	Lua.cs \
	Metatables.cs \
	ObjectTranslator.cs \
	ProxyType.cs \
	Properties/AssemblyInfo.cs \
	LuaBase.cs \
	LuaFunction.cs \
	LuaGlobalAttribute.cs \
	LuaHideAttribute.cs \
	LuaRegistrationHelper.cs \
	LuaTable.cs \
	LuaUserData.cs \
	Extensions/GeneralExtensions.cs \
	GenerateEventAssembly/LuaClassType.cs \
	GenerateEventAssembly/ILuaGeneratedType.cs \
	GenerateEventAssembly/DelegateGenerator.cs \
	GenerateEventAssembly/ClassGenerator.cs \
	GenerateEventAssembly/CodeGeneration.cs \
	Event/EventCodes.cs \
	Event/EventMasks.cs \
	Event/DebugHookEventArgs.cs \
	Event/HookExceptionEventArgs.cs \
	Exceptions/LuaException.cs \
	Exceptions/LuaScriptException.cs \
	LuaLib/LuaEnums.cs \
	LuaLib/References.cs \
	LuaLib/LuaTypes.cs \
	Method/MethodCache.cs \
	Method/MethodArgs.cs \
	Method/LuaMethodWrapper.cs \
	Method/EventHandlerContainer.cs \
	Method/RegisterEventHandler.cs \
	Method/LuaEventHandler.cs \
	Method/LuaDelegate.cs \
	Method/LuaClassHelper.cs \
	LuaLib/LuaIndexes.cs \
	LuaLib/GCOptions.cs \
	LuaLib/LuaLib.cs \
	Config/NLuaConfig.cs \
	ObjectTranslatorPool.cs 

DATA_FILES = 

RESOURCES = 

EXTRAS = \
	nlua.pc.in 

REFERENCES =  \
	System \
	System.Data \
	System.Xml \
	System.Core

DLL_REFERENCES = 

CLEANFILES = $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

include $(top_srcdir)/Makefile.include

KERALUA_DLL = $(BUILD_DIR)/KeraLua.dll
KOPILUA_DLL = $(BUILD_DIR)/KopiLua.dll
NLUA_PC = $(BUILD_DIR)/nlua.pc

$(eval $(call emit-deploy-target,KERALUA_DLL))
$(eval $(call emit-deploy-target,KERALUA_DLL_MDB))
$(eval $(call emit-deploy-target,KOPILUA_DLL))
$(eval $(call emit-deploy-target,KOPILUA_DLL_MDB))
$(eval $(call emit-deploy-wrapper,NLUA_PC,nlua.pc))


$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(ASSEMBLY_MDB): $(ASSEMBLY)

$(ASSEMBLY): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(shell dirname $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
