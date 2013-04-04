SHARED_SRC_DIR = ../shared
SOURCES = \
	AssemblyInfo.cs \
	ClusteredBoard.cs \
	Main.cs \
	PixelSettings.cs \
	PixelWindow.cs \
	PlayerPixel.cs \
	$(SHARED_SRC_DIR)/CLIObject.cs \
	$(SHARED_SRC_DIR)/DefaultGameSettings.cs \
	$(SHARED_SRC_DIR)/DoubleAnimator.cs \
	$(SHARED_SRC_DIR)/TexturesCompat.cs \
	$(SHARED_SRC_DIR)/Themes.cs \
	$(SHARED_SRC_DIR)/YAMLOutputStream.cs

LIB_DIR = ../lib
LIBS = \
	-r:System \
	-r:System.Core \
	-r:System.Drawing \
	-r:$(LIB_DIR)/log4net.dll \
	-r:$(LIB_DIR)/NDesk.Options.dll \
	-r:$(LIB_DIR)/OpenTK.dll \
	-r:$(LIB_DIR)/OpenTK.Compatibility.dll

BUNDLED_FILES = \
	$(LIB_DIR)/log4net.dll \
	$(LIB_DIR)/NDesk.Options.dll \
	$(LIB_DIR)/OpenTK.dll \
	$(LIB_DIR)/OpenTK.dll.config \
	$(LIB_DIR)/OpenTK.Compatibility.dll \
	$(LIB_DIR)/OpenTK.Compatibility.dll.config

OUTPUT_DIR = ../bin/Debug
OUTPUT = $(OUTPUT_DIR)/Pixel.exe

all: $(SOURCES)
	mkdir -p $(OUTPUT_DIR);
	mono-csc -target:winexe -debug:+ -debug:full -out:$(OUTPUT) $(LIBS) $(SOURCES);
	cp $(BUNDLED_FILES) $(OUTPUT_DIR);

clean:
	rm $(OUTPUT)
