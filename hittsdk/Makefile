SOURCES = \
	BaseStation.cs \
	DefaultAPI.cs \
	DefaultSerialPort.cs \
	IHITTAPI.cs \
	ISerialPort.cs \
	MacAPI.cs \
	MacSerialPort.cs \
	Platform.cs

LIB_DIR = ../lib
LIBS = \
	-r:System \
	-r:System.Core \
	-r:Mono.Posix \
	-r:$(LIB_DIR)/log4net.dll

OUTPUT_DIR = $(LIB_DIR)
OUTPUT = $(OUTPUT_DIR)/HITTSDK.dll

$(OUTPUT) : $(SOURCES)
	mono-csc -target:library -debug:+ -debug:full -out:$(OUTPUT) $(LIBS) $(SOURCES);

clean:
	rm $(OUTPUT)
