SOURCES = \
	AssemblyInfo.cs \
	DialogEditFixedTiles.cs \
	DialogEditGame.cs \
	DialogNewGame.cs \
	Main.cs \
	MainWindow.cs \
	Settings.cs \
	games/FlatlandGameInfo.cs \
	games/ForagerGameInfo.cs \
	games/GroupSumGameInfo.cs \
	games/IGameInfo.cs \
	games/PixelGameInfo.cs \
	gtk-gui/generated.cs \
	gtk-gui/MainWindow.cs \
	gtk-gui/Nexus.DialogNewGame.cs \
	gtk-gui/Nexus.DialogEditFixedTiles.cs \
	shared/CLIObject.cs \
	shared/Themes.cs \
	utility/DrawingAreaGrid.cs \
	utility/GTKUtility.cs \
	utility/HITTServer.cs \
	utility/Markdown.cs \
	utility/Images.cs

LIB_DIR = lib
LIBS = \
	-r:System \
	-r:System.Core \
	-r:System.Drawing \
	-r:System.Xml \
	-r:System.Xml.Linq \
	-r:Mono.Cairo \
	-r:Mono.Posix \
	-pkg:gtk-sharp-2.0 \
	-r:$(LIB_DIR)/HITTSDK.dll \
	-r:$(LIB_DIR)/log4net.dll
	#-r:$(LIB_DIR)/WIICWrapper.dll \
	#-r:$(LIB_DIR)/Tao.Sdl.dll

BUNDLED_FILES = \
	etc \
	$(LIB_DIR)/HITTSDK.dll \
	$(LIB_DIR)/HITTSDK.dll.config \
	$(LIB_DIR)/log4net.dll \
	$(LIB_DIR)/NDesk.Options.dll \
	$(LIB_DIR)/NPlot.dll \
	$(LIB_DIR)/OpenTK.dll \
	$(LIB_DIR)/OpenTK.dll.config \
	$(LIB_DIR)/OpenTK.Compatibility.dll \
	$(LIB_DIR)/OpenTK.Compatibility.dll.config \
	$(LIB_DIR)/native/libmacserial.dylib \
	$(LIB_DIR)/native/libh-ittsdkwrapper.dylib \
	$(LIB_DIR)/native/libH-ITTSDK.so \
	$(LIB_DIR)/native/libH-ITTSDK_m64.so \
	$(LIB_DIR)/native/libH-ITTSDK-mac.so
	#$(LIB_DIR)/WIICWrapper.dll \
	#$(LIB_DIR)/WIICClient.exe \
	#$(LIB_DIR)/native/libwiic.so \
	#$(LIB_DIR)/native/libwiic_wrapper.so \
	#$(LIB_DIR)/native/libwiic.dylib \
	#$(LIB_DIR)/native/libwiic_wrapper.dylib \
	#$(LIB_DIR)/native/wiic_wrapper.dll.config \
	#$(LIB_DIR)/Tao.Sdl.dll \
	#$(LIB_DIR)/Tao.Sdl.dll.config

OUTPUT_DIR = bin/Debug
OUTPUT = $(OUTPUT_DIR)/Nexus.exe
MAC_APP_PATH = $(OUTPUT_DIR)/Nexus.app
MAC_APP_RESOURCES = \
	$(BUNDLED_FILES) \
	$(OUTPUT_DIR)/Flatland.exe \
	$(OUTPUT_DIR)/Forager.exe \
	$(OUTPUT_DIR)/GroupSum.exe \
	$(OUTPUT_DIR)/Pixel.exe \
	$(OUTPUT_DIR)/Nexus.exe \
	SerialTest/bin/Debug/SerialTest.exe

WIN_ZIP_PATH = $(OUTPUT_DIR)/Nexus_win.zip
WIN_FILES = \
	$(LIB_DIR)/HITTSDK.dll \
	$(LIB_DIR)/HITTSDK.dll.config \
	$(LIB_DIR)/log4net.dll \
	$(LIB_DIR)/NDesk.Options.dll \
	$(LIB_DIR)/NPlot.dll \
	$(LIB_DIR)/OpenTK.dll \
	$(LIB_DIR)/OpenTK.dll.config \
	$(LIB_DIR)/OpenTK.Compatibility.dll \
	$(LIB_DIR)/OpenTK.Compatibility.dll.config \
	$(LIB_DIR)/native/h-ittsdk.dll \
	$(LIB_DIR)/native/h-ittsdk_m64.dll \
	$(OUTPUT_DIR)/Flatland.exe \
	$(OUTPUT_DIR)/Forager.exe \
	$(OUTPUT_DIR)/GroupSum.exe \
	$(OUTPUT_DIR)/Pixel.exe \
	$(OUTPUT_DIR)/Nexus.exe
	#$(LIB_DIR)/WIICWrapper.dll \
	#$(LIB_DIR)/WIICClient.exe \

$(OUTPUT) : $(SOURCES)
	mkdir -p $(OUTPUT_DIR);
	$(MAKE) -C "hittsdk";
	$(MAKE) -C "SerialTest";
	$(MAKE) -C "Flatland";
	$(MAKE) -C "Forager";
	$(MAKE) -C "GroupSum";
	$(MAKE) -C "Pixel";
	mono-csc -target:winexe -debug:+ -debug:full -out:$(OUTPUT) $(LIBS) $(SOURCES);
	cp -r $(BUNDLED_FILES) $(OUTPUT_DIR);	

clean:
	rm $(OUTPUT)

mac: $(OUTPUT)
	rm -rf $(MAC_APP_PATH);
	mkdir -p $(MAC_APP_PATH);
	mkdir -p $(MAC_APP_PATH)/Contents;
	cp etc/Info.plist $(MAC_APP_PATH)/Contents;

	mkdir -p $(MAC_APP_PATH)/Contents/Resources;
	cp etc/icon.icns $(MAC_APP_PATH)/Contents/Resources;

	mkdir -p $(MAC_APP_PATH)/Contents/MacOS;
	cp etc/nexus $(MAC_APP_PATH)/Contents/MacOS;
	cp -R $(MAC_APP_RESOURCES) $(MAC_APP_PATH)/Contents/MacOS;

	cd $(OUTPUT_DIR); rm -f Nexus.app.zip; zip -r Nexus.app.zip Nexus.app

	#macpack -n:Nexus -o:$(OUTPUT_DIR) -a:$(OUTPUT) -m:2 $(MAC_APP_RESOURCES) -i:etc/icon.icns;

windows: $(OUTPUT)
	rm -rf $(WIN_ZIP_PATH)
	zip -j $(WIN_ZIP_PATH) $(WIN_FILES)
	zip -ur $(WIN_ZIP_PATH) etc
