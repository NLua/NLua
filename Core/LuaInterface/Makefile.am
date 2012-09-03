
EXTRA_DIST =  

# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG_X86
ASSEMBLY_COMPILER_COMMAND = dmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize- -debug "-define:DEBUG"
ASSEMBLY = ../../Run/Debug/LuaInterface.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES =  \
	../../Run/Debug/KopiLua.dll
BUILD_DIR = ../../Run/Debug

LUAINTERFACE_DLL_MDB_SOURCE=../../Run/Debug/LuaInterface.dll.mdb
LUAINTERFACE_DLL_MDB=$(BUILD_DIR)/LuaInterface.dll.mdb
KOPILUA_DLL_SOURCE=../../Run/Debug/KopiLua.dll

endif

if ENABLE_RELEASE_X86
ASSEMBLY_COMPILER_COMMAND = dmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ "-define:RELEASE"
ASSEMBLY = ../../Run/Release/LuaInterface.dll
ASSEMBLY_MDB = 
COMPILE_TARGET = library
PROJECT_REFERENCES =  \
	../../Run/Release/KopiLua.dll
BUILD_DIR = ../../Run/Release

LUAINTERFACE_DLL_MDB=
KOPILUA_DLL_SOURCE=../../Run/Release/KopiLua.dll

endif

if ENABLE_DEBUG_X64
ASSEMBLY_COMPILER_COMMAND = dmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize- -debug "-define:DEBUG"
ASSEMBLY = ../../Run/Debug_x64/LuaInterface.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES =  \
	../../Run/Debug_x64/KopiLua.dll
BUILD_DIR = ../../Run/Debug_x64

LUAINTERFACE_DLL_MDB_SOURCE=../../Run/Debug_x64/LuaInterface.dll.mdb
LUAINTERFACE_DLL_MDB=$(BUILD_DIR)/LuaInterface.dll.mdb
KOPILUA_DLL_SOURCE=../../Run/Debug_x64/KopiLua.dll

endif

if ENABLE_RELEASE_X64
ASSEMBLY_COMPILER_COMMAND = dmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ "-define:RELEASE"
ASSEMBLY = ../../Run/Release_x64/LuaInterface.dll
ASSEMBLY_MDB = 
COMPILE_TARGET = library
PROJECT_REFERENCES =  \
	../../Run/Release_x64/KopiLua.dll
BUILD_DIR = ../../Run/Release_x64

LUAINTERFACE_DLL_MDB=
KOPILUA_DLL_SOURCE=../../Run/Release_x64/KopiLua.dll

endif

AL=al
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES = \
	$(LUAINTERFACE_DLL_MDB) \
	$(KOPILUA_DLL)  

LINUX_PKGCONFIG = \
	$(LUAINTERFACE_PC)  


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
	Config/LuaInterfaceConfig.cs 

DATA_FILES = 

RESOURCES = 

EXTRAS = \
	Extensions \
	GenerateEventAssembly \
	Event \
	Exceptions \
	Method \
	Config \
	luainterface.pc.in 

REFERENCES =  \
	System \
	System.Data \
	System.Xml

DLL_REFERENCES = 

CLEANFILES = $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

include $(top_srcdir)/Makefile.include

KOPILUA_DLL = $(BUILD_DIR)/KopiLua.dll
LUAINTERFACE_PC = $(BUILD_DIR)/luainterface.pc

$(eval $(call emit-deploy-wrapper,LUAINTERFACE_PC,luainterface.pc))


$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(ASSEMBLY_MDB): $(ASSEMBLY)

$(ASSEMBLY): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(shell dirname $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
