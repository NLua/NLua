
EXTRA_DIST =  

# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize- -debug "-define:DEBUG;TRACE"
ASSEMBLY = ../tests/NLuaTest.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES =  \
	../Core/KeraLua/src/bin/Debug/KeraLua.dll \
	../Core/KopiLua/Bin/Debug/KopiLua.dll \
	../Run/Debug/NLua.dll
BUILD_DIR = ../tests/

NLUATEST_DLL_MDB_SOURCE=../tests/NLuaTest.dll.mdb
NLUATEST_DLL_MDB=$(BUILD_DIR)/NLuaTest.dll.mdb
NUNIT_FRAMEWORK_DLL_SOURCE=../lib/nunit/nunit.framework.dll
KERALUA_DLL_SOURCE=../Core/KeraLua/src/bin/Debug/KeraLua.dll
KERALUA_DLL_MDB_SOURCE=../Core/KeraLua/src/bin/Debug/KeraLua.dll.mdb
KERALUA_DLL_MDB=$(BUILD_DIR)/KeraLua.dll.mdb
KOPILUA_DLL_SOURCE=../Core/KopiLua/Bin/Debug/KopiLua.dll
KOPILUA_DLL_MDB_SOURCE=../Core/KopiLua/Bin/Debug/KopiLua.dll.mdb
KOPILUA_DLL_MDB=$(BUILD_DIR)/KopiLua.dll.mdb
NLUA_DLL_SOURCE=../Run/Debug/NLua.dll
NLUA_DLL_MDB_SOURCE=../Run/Debug/NLua.dll.mdb
NLUA_DLL_MDB=$(BUILD_DIR)/NLua.dll.mdb

endif

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ "-define:TRACE"
ASSEMBLY = ../tests/NLuaTest.dll
ASSEMBLY_MDB = 
COMPILE_TARGET = library
PROJECT_REFERENCES =  \
	../Core/KeraLua/src/bin/Release/KeraLua.dll \
	../Core/KopiLua/Bin/Release/KopiLua.dll \
	../Run/Release/NLua.dll
BUILD_DIR = ../tests/

NLUATEST_DLL_MDB=
NUNIT_FRAMEWORK_DLL_SOURCE=../lib/nunit/nunit.framework.dll
KERALUA_DLL_SOURCE=../Core/KeraLua/src/bin/Release/KeraLua.dll
KERALUA_DLL_MDB=
KOPILUA_DLL_SOURCE=../Core/KopiLua/Bin/Release/KopiLua.dll
KOPILUA_DLL_MDB=
NLUA_DLL_SOURCE=../Run/Release/NLua.dll
NLUA_DLL_MDB=

endif

AL=al2
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES = \
	$(NLUATEST_DLL_MDB) \
	$(NUNIT_FRAMEWORK_DLL) \
	$(KERALUA_DLL) \
	$(KERALUA_DLL_MDB) \
	$(KOPILUA_DLL) \
	$(KOPILUA_DLL_MDB) \
	$(NLUA_DLL) \
	$(NLUA_DLL_MDB)  

LINUX_PKGCONFIG = \
	$(NLUATEST_PC)  


RESGEN=resgen2
	
all: $(ASSEMBLY) $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

FILES = \
	../tests/Entity.cs \
	../tests/LuaTests.cs \
	../tests/TestLua.cs \
	../tests/Core.cs \
	LoadFileTests.cs \
	Properties/AssemblyInfo.cs 

DATA_FILES = 

RESOURCES = 

EXTRAS = \
	nluatest.pc.in 

REFERENCES =  \
	System \
	System.Data \
	System.Xml

DLL_REFERENCES =  \
	../lib/nunit/nunit.framework.dll

CLEANFILES = $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

include $(top_srcdir)/Makefile.include

NUNIT_FRAMEWORK_DLL = $(BUILD_DIR)/nunit.framework.dll
KERALUA_DLL = $(BUILD_DIR)/KeraLua.dll
KOPILUA_DLL = $(BUILD_DIR)/KopiLua.dll
NLUA_DLL = $(BUILD_DIR)/NLua.dll
NLUATEST_PC = $(BUILD_DIR)/nluatest.pc

$(eval $(call emit-deploy-target,NUNIT_FRAMEWORK_DLL))
$(eval $(call emit-deploy-target,KERALUA_DLL))
$(eval $(call emit-deploy-target,KERALUA_DLL_MDB))
$(eval $(call emit-deploy-target,KOPILUA_DLL))
$(eval $(call emit-deploy-target,KOPILUA_DLL_MDB))
$(eval $(call emit-deploy-target,NLUA_DLL))
$(eval $(call emit-deploy-target,NLUA_DLL_MDB))
$(eval $(call emit-deploy-wrapper,NLUATEST_PC,nluatest.pc))


$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(ASSEMBLY_MDB): $(ASSEMBLY)

$(ASSEMBLY): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(shell dirname $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
