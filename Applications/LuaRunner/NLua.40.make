

# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = dmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize- -debug "-define:DEBUG"
ASSEMBLY = ../../tests/NLua.exe
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = exe
PROJECT_REFERENCES =  \
	../../Run/Debug/net40/NLua.dll
BUILD_DIR = ../../tests/

NLUA_EXE_MDB_SOURCE=../../tests/NLua.exe.mdb
NLUA_EXE_MDB=$(BUILD_DIR)/NLua.exe.mdb
NLUA_EXE_CONFIG_SOURCE=app.config
NLUA_DLL_SOURCE=../../Run/Debug/net40/NLua.dll
NLUA_DLL_MDB_SOURCE=../../Run/Debug/net40/NLua.dll.mdb
NLUA_DLL_MDB=$(BUILD_DIR)/NLua.dll.mdb
KERALUA_DLL_SOURCE=../../Core/KeraLua/src/bin/Debug/net40/KeraLua.dll
KERALUA_DLL_MDB_SOURCE=../../Core/KeraLua/src/bin/Debug/net40/KeraLua.dll.mdb
KERALUA_DLL_MDB=$(BUILD_DIR)/KeraLua.dll.mdb
KOPILUA_DLL_SOURCE=../../Core/KopiLua/Bin/Debug/net40/KopiLua.dll
KOPILUA_DLL_MDB_SOURCE=../../Core/KopiLua/Bin/Debug/net40/KopiLua.dll.mdb
KOPILUA_DLL_MDB=$(BUILD_DIR)/KopiLua.dll.mdb

endif

if ENABLE_DEBUGKOPILUA
ASSEMBLY_COMPILER_COMMAND = dmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize- -debug "-define:DEBUG"
ASSEMBLY = ../../tests/NLua.exe
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = exe
PROJECT_REFERENCES =  \
	../../Core/NLua/bin/DebugKopiLua/NLua.dll
BUILD_DIR = ../../tests/

NLUA_EXE_MDB_SOURCE=../../tests/NLua.exe.mdb
NLUA_EXE_MDB=$(BUILD_DIR)/NLua.exe.mdb
NLUA_EXE_CONFIG_SOURCE=app.config
NLUA_DLL_SOURCE=../../Core/NLua/bin/DebugKopiLua/NLua.dll
NLUA_DLL_MDB_SOURCE=../../Core/NLua/bin/DebugKopiLua/NLua.dll.mdb
NLUA_DLL_MDB=$(BUILD_DIR)/NLua.dll.mdb
KERALUA_DLL_SOURCE=../../Core/KeraLua/src/bin/DebugKopiLua/KeraLua.dll
KERALUA_DLL_MDB_SOURCE=../../Core/KeraLua/src/bin/DebugKopiLua/KeraLua.dll.mdb
KERALUA_DLL_MDB=$(BUILD_DIR)/KeraLua.dll.mdb
KOPILUA_DLL_SOURCE=../../Core/KopiLua/KopiLua/bin/DebugKopiLua/KopiLua.dll
KOPILUA_DLL_MDB_SOURCE=../../Core/KopiLua/KopiLua/bin/DebugKopiLua/KopiLua.dll.mdb
KOPILUA_DLL_MDB=$(BUILD_DIR)/KopiLua.dll.mdb

endif

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = dmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ "-define:RELEASE"
ASSEMBLY = ../../tests/NLua.exe
ASSEMBLY_MDB = 
COMPILE_TARGET = exe
PROJECT_REFERENCES =  \
	../../Run/Release/net40/NLua.dll
BUILD_DIR = ../../tests/

NLUA_EXE_MDB=
NLUA_EXE_CONFIG_SOURCE=app.config
NLUA_DLL_SOURCE=../../Run/Release/net40/NLua.dll
NLUA_DLL_MDB=
KERALUA_DLL_SOURCE=../../Core/KeraLua/src/bin/Release/net40/KeraLua.dll
KERALUA_DLL_MDB=
KOPILUA_DLL_SOURCE=../../Core/KopiLua/Bin/Release/net40/KopiLua.dll
KOPILUA_DLL_MDB=

endif

if ENABLE_RELEASEKOPILUA
ASSEMBLY_COMPILER_COMMAND = dmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize-
ASSEMBLY = ../../tests/NLua.exe
ASSEMBLY_MDB = 
COMPILE_TARGET = exe
PROJECT_REFERENCES =  \
	../../Core/NLua/bin/ReleaseKopiLua/NLua.dll
BUILD_DIR = ../../tests/

NLUA_EXE_MDB=
NLUA_EXE_CONFIG_SOURCE=app.config
NLUA_DLL_SOURCE=../../Core/NLua/bin/ReleaseKopiLua/NLua.dll
NLUA_DLL_MDB=
KERALUA_DLL_SOURCE=../../Core/KeraLua/src/bin/ReleaseKopiLua/KeraLua.dll
KERALUA_DLL_MDB=
KOPILUA_DLL_SOURCE=../../Core/KopiLua/KopiLua/bin/ReleaseKopiLua/KopiLua.dll
KOPILUA_DLL_MDB=

endif

if ENABLE_DEBUG_X64
ASSEMBLY_COMPILER_COMMAND = dmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize- -debug "-define:DEBUG"
ASSEMBLY = ../../Run/Debug_x64/NLua.exe
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = exe
PROJECT_REFERENCES =  \
	../../Run/Debug/net40/NLua.dll
BUILD_DIR = ../../Run/Debug_x64

NLUA_EXE_MDB_SOURCE=../../Run/Debug_x64/NLua.exe.mdb
NLUA_EXE_MDB=$(BUILD_DIR)/NLua.exe.mdb
NLUA_EXE_CONFIG_SOURCE=app.config
NLUA_DLL_SOURCE=../../Run/Debug/net40/NLua.dll
NLUA_DLL_MDB_SOURCE=../../Run/Debug/net40/NLua.dll.mdb
NLUA_DLL_MDB=$(BUILD_DIR)/NLua.dll.mdb
KERALUA_DLL_SOURCE=../../Core/KeraLua/src/bin/Debug/net40/KeraLua.dll
KERALUA_DLL_MDB_SOURCE=../../Core/KeraLua/src/bin/Debug/net40/KeraLua.dll.mdb
KERALUA_DLL_MDB=$(BUILD_DIR)/KeraLua.dll.mdb
KOPILUA_DLL_SOURCE=../../Core/KopiLua/Bin/Debug/net40/KopiLua.dll
KOPILUA_DLL_MDB_SOURCE=../../Core/KopiLua/Bin/Debug/net40/KopiLua.dll.mdb
KOPILUA_DLL_MDB=$(BUILD_DIR)/KopiLua.dll.mdb

endif

if ENABLE_RELEASE_X64
ASSEMBLY_COMPILER_COMMAND = dmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ "-define:RELEASE"
ASSEMBLY = ../../Run/Release_x64/NLua.exe
ASSEMBLY_MDB = 
COMPILE_TARGET = exe
PROJECT_REFERENCES =  \
	../../Run/Release/net40/NLua.dll
BUILD_DIR = ../../Run/Release_x64

NLUA_EXE_MDB=
NLUA_EXE_CONFIG_SOURCE=app.config
NLUA_DLL_SOURCE=../../Run/Debug/net40/NLua.dll
NLUA_DLL_MDB_SOURCE=../../Run/Debug/net40/NLua.dll.mdb
NLUA_DLL_MDB=$(BUILD_DIR)/NLua.dll.mdb
KERALUA_DLL_SOURCE=../../Core/KeraLua/src/bin/Debug/net40/KeraLua.dll
KERALUA_DLL_MDB_SOURCE=../../Core/KeraLua/src/bin/Debug/net40/KeraLua.dll.mdb
KERALUA_DLL_MDB=$(BUILD_DIR)/KeraLua.dll.mdb
KOPILUA_DLL_SOURCE=../../Core/KopiLua/Bin/Debug/net40/KopiLua.dll
KOPILUA_DLL_MDB_SOURCE=../../Core/KopiLua/Bin/Debug/net40/KopiLua.dll.mdb
KOPILUA_DLL_MDB=$(BUILD_DIR)/KopiLua.dll.mdb

endif

if ENABLE_DEBUGKOPILUA_X64
ASSEMBLY_COMPILER_COMMAND = dmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize- -debug "-define:DEBUG"
ASSEMBLY = bin/x64/DebugKopiLua/NLua.exe
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = exe
PROJECT_REFERENCES =  \
	../../Core/NLua/bin/DebugKopiLua/NLua.dll
BUILD_DIR = bin/x64/DebugKopiLua/

NLUA_EXE_MDB_SOURCE=bin/x64/DebugKopiLua/NLua.exe.mdb
NLUA_EXE_MDB=$(BUILD_DIR)/NLua.exe.mdb
NLUA_EXE_CONFIG_SOURCE=app.config
NLUA_DLL_SOURCE=../../Run/Debug/net40/NLua.dll
NLUA_DLL_MDB_SOURCE=../../Run/Debug/net40/NLua.dll.mdb
NLUA_DLL_MDB=$(BUILD_DIR)/NLua.dll.mdb
KERALUA_DLL_SOURCE=../../Core/KeraLua/src/bin/Debug/net40/KeraLua.dll
KERALUA_DLL_MDB_SOURCE=../../Core/KeraLua/src/bin/Debug/net40/KeraLua.dll.mdb
KERALUA_DLL_MDB=$(BUILD_DIR)/KeraLua.dll.mdb
KOPILUA_DLL_SOURCE=../../Core/KopiLua/Bin/Debug/net40/KopiLua.dll
KOPILUA_DLL_MDB_SOURCE=../../Core/KopiLua/Bin/Debug/net40/KopiLua.dll.mdb
KOPILUA_DLL_MDB=$(BUILD_DIR)/KopiLua.dll.mdb

endif

if ENABLE_RELEASEKOPILUA_X64
ASSEMBLY_COMPILER_COMMAND = dmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize-
ASSEMBLY = bin/x64/ReleaseKopiLua/NLua.exe
ASSEMBLY_MDB = 
COMPILE_TARGET = exe
PROJECT_REFERENCES =  \
	../../Core/NLua/bin/ReleaseKopiLua/NLua.dll
BUILD_DIR = bin/x64/ReleaseKopiLua/

NLUA_EXE_MDB=
NLUA_EXE_CONFIG_SOURCE=app.config
NLUA_DLL_SOURCE=../../Run/Debug/net40/NLua.dll
NLUA_DLL_MDB_SOURCE=../../Run/Debug/net40/NLua.dll.mdb
NLUA_DLL_MDB=$(BUILD_DIR)/NLua.dll.mdb
KERALUA_DLL_SOURCE=../../Core/KeraLua/src/bin/Debug/net40/KeraLua.dll
KERALUA_DLL_MDB_SOURCE=../../Core/KeraLua/src/bin/Debug/net40/KeraLua.dll.mdb
KERALUA_DLL_MDB=$(BUILD_DIR)/KeraLua.dll.mdb
KOPILUA_DLL_SOURCE=../../Core/KopiLua/Bin/Debug/net40/KopiLua.dll
KOPILUA_DLL_MDB_SOURCE=../../Core/KopiLua/Bin/Debug/net40/KopiLua.dll.mdb
KOPILUA_DLL_MDB=$(BUILD_DIR)/KopiLua.dll.mdb

endif

AL=al
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES = \
	$(NLUA_EXE_MDB) \
	$(NLUA_EXE_CONFIG) \
	$(NLUA_DLL) \
	$(NLUA_DLL_MDB) \
	$(KERALUA_DLL) \
	$(KERALUA_DLL_MDB) \
	$(KOPILUA_DLL) \
	$(KOPILUA_DLL_MDB)  

BINARIES = \
	$(NLUA_40)  


RESGEN=resgen2
	
all: $(ASSEMBLY) $(PROGRAMFILES) $(BINARIES) 

FILES = \
	LuaNetRunner.cs \
	Properties/AssemblyInfo.cs 

DATA_FILES = 

RESOURCES = 

EXTRAS = \
	app.config \
	NLua.ico \
	nlua.40.in 

REFERENCES =  \
	System \
	System.Data \
	System.Xml

DLL_REFERENCES = 

CLEANFILES = $(PROGRAMFILES) $(BINARIES) 

include $(top_srcdir)/Makefile.include

NLUA_EXE_CONFIG = $(BUILD_DIR)/NLua.exe.config
NLUA_DLL = $(BUILD_DIR)/NLua.dll
KERALUA_DLL = $(BUILD_DIR)/KeraLua.dll
KOPILUA_DLL = $(BUILD_DIR)/KopiLua.dll
NLUA_40 = $(BUILD_DIR)/nlua.40

$(eval $(call emit-deploy-target,NLUA_EXE_CONFIG))
$(eval $(call emit-deploy-target,NLUA_DLL))
$(eval $(call emit-deploy-target,NLUA_DLL_MDB))
$(eval $(call emit-deploy-target,KERALUA_DLL))
$(eval $(call emit-deploy-target,KERALUA_DLL_MDB))
$(eval $(call emit-deploy-target,KOPILUA_DLL))
$(eval $(call emit-deploy-target,KOPILUA_DLL_MDB))
$(eval $(call emit-deploy-wrapper,NLUA_40,nlua.40,x))


$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(ASSEMBLY_MDB): $(ASSEMBLY)

$(ASSEMBLY): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(shell dirname $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
