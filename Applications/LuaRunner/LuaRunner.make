

# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG_X86
ASSEMBLY_COMPILER_COMMAND = dmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize- -debug "-define:DEBUG"
ASSEMBLY = ../../Run/Debug/LuaRunner.exe
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = exe
PROJECT_REFERENCES =  \
	../../Run/Debug/LuaInterface.dll
BUILD_DIR = ../../Run/Debug

LUARUNNER_EXE_MDB_SOURCE=../../Run/Debug/LuaRunner.exe.mdb
LUARUNNER_EXE_MDB=$(BUILD_DIR)/LuaRunner.exe.mdb
LUAINTERFACE_DLL_SOURCE=../../Run/Debug/LuaInterface.dll
KOPILUA_DLL_SOURCE=../../Run/Debug/KopiLua.dll

endif

if ENABLE_RELEASE_X86
ASSEMBLY_COMPILER_COMMAND = dmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ "-define:RELEASE"
ASSEMBLY = ../../Run/Release/LuaRunner.exe
ASSEMBLY_MDB = 
COMPILE_TARGET = exe
PROJECT_REFERENCES =  \
	../../Run/Release/LuaInterface.dll
BUILD_DIR = ../../Run/Release

LUARUNNER_EXE_MDB=
LUAINTERFACE_DLL_SOURCE=../../Run/Release/LuaInterface.dll
KOPILUA_DLL_SOURCE=../../Run/Release/KopiLua.dll

endif

if ENABLE_DEBUG_X64
ASSEMBLY_COMPILER_COMMAND = dmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize- -debug "-define:DEBUG"
ASSEMBLY = ../../Run/Debug_x64/LuaRunner.exe
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = exe
PROJECT_REFERENCES =  \
	../../Run/Debug_x64/LuaInterface.dll
BUILD_DIR = ../../Run/Debug_x64

LUARUNNER_EXE_MDB_SOURCE=../../Run/Debug_x64/LuaRunner.exe.mdb
LUARUNNER_EXE_MDB=$(BUILD_DIR)/LuaRunner.exe.mdb
LUAINTERFACE_DLL_SOURCE=../../Run/Debug_x64/LuaInterface.dll
KOPILUA_DLL_SOURCE=../../Run/Debug_x64/KopiLua.dll

endif

if ENABLE_RELEASE_X64
ASSEMBLY_COMPILER_COMMAND = dmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ "-define:RELEASE"
ASSEMBLY = ../../Run/Release_x64/LuaRunner.exe
ASSEMBLY_MDB = 
COMPILE_TARGET = exe
PROJECT_REFERENCES =  \
	../../Run/Release_x64/LuaInterface.dll
BUILD_DIR = ../../Run/Release_x64

LUARUNNER_EXE_MDB=
LUAINTERFACE_DLL_SOURCE=../../Run/Release_x64/LuaInterface.dll
KOPILUA_DLL_SOURCE=../../Run/Release_x64/KopiLua.dll

endif

AL=al
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES = \
	$(LUARUNNER_EXE_MDB) \
	$(LUAINTERFACE_DLL) \
	$(KOPILUA_DLL)  

BINARIES = \
	$(LUARUNNER)  


RESGEN=resgen2
	
all: $(ASSEMBLY) $(PROGRAMFILES) $(BINARIES) 

FILES = \
	LuaNetRunner.cs \
	Properties/AssemblyInfo.cs 

DATA_FILES = 

RESOURCES = 

EXTRAS = \
	luarunner.in 

REFERENCES =  \
	System \
	System.Data \
	System.Xml

DLL_REFERENCES = 

CLEANFILES = $(PROGRAMFILES) $(BINARIES) 

include $(top_srcdir)/Makefile.include

LUAINTERFACE_DLL = $(BUILD_DIR)/LuaInterface.dll
KOPILUA_DLL = $(BUILD_DIR)/KopiLua.dll
LUARUNNER = $(BUILD_DIR)/luarunner

$(eval $(call emit-deploy-wrapper,LUARUNNER,luarunner,x))


$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(ASSEMBLY_MDB): $(ASSEMBLY)

$(ASSEMBLY): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(shell dirname $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
