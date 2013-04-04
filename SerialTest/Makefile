SOURCES = \
	Program.cs

LIB_DIR = ../lib
LIBS = \
	-r:System \
	-r:System.Core \
	-r:$(LIB_DIR)/HITTSDK.dll

OUTPUT_DIR = bin/Debug
OUTPUT = $(OUTPUT_DIR)/SerialTest.exe

$(OUTPUT) : $(SOURCES)
	mkdir -p $(OUTPUT_DIR)
	mono-csc -target:exe -debug:+ -debug:full -out:$(OUTPUT) $(LIBS) $(SOURCES);
	cp $(LIB_DIR)/HITTSDK.dll $(OUTPUT_DIR)
	cp $(LIB_DIR)/HITTSDK.dll.config $(OUTPUT_DIR)
	cp $(LIB_DIR)/log4net.dll $(OUTPUT_DIR)

clean:
	rm $(OUTPUT)
