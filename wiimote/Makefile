all: wiic_wrapper WIICWrapper

#WIICClient: WIICWrapper
	#mkdir -p bin
	#gmcs -target:winexe -debug:+ -debug:full -out:bin/WIICClient.exe -r:mscorlib WIICWrapper.cs WIICClient.cs
	#cp -u bin/WIICClient.exe ../lib/

WIICWrapper: wiic_wrapper
	mkdir -p bin
	gmcs -target:library -debug:+ -debug:full -out:bin/WIICWrapper.dll -r:mscorlib WIICWrapper.cs;
	cp -u wiic_wrapper.dll.config bin/
	cp -u wiic_wrapper.dll.config ../lib/native/
	cp -u bin/WIICWrapper.dll ../lib/
	cp -u bin/WIICWrapper.dll ../bin/Debug/

wiic_wrapper: wiic_wrapper.c
	mkdir -p bin
	gcc -DLINUX --shared -fPIC -lwiic -lrt -Llib/ -o bin/libwiic_wrapper.so wiic_wrapper.c
	cp -u bin/libwiic_wrapper.so ../lib/native/
	cp -u bin/libwiic_wrapper.so ../bin/Debug/
	cp -u bin/libwiic.so ../bin/Debug/

test: test.c
	mkdir -p bin
	gcc -DLINUX -lwiic -Llib -Isrc -o bin/test test.c
	cp -u lib/libwiic.so bin/
