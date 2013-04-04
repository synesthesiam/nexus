SOURCES = *.cs Properties/*.cs
BIN = bin/Debug/NPlot.dll

$(BIN): $(SOURCES)
	gmcs -t:library -out:$(BIN) $(SOURCES)

clean:
	rm $(BIN)
