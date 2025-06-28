PLUGIN_NAME=ProgressMap
BUILD_DIR=.\obj\Debug\netstandard2.1
PLUGIN_DIR=..\..\plugins

all: clean build move

clean:
	del /q $(PLUGIN_DIR)\$(PLUGIN_NAME).dll 2>nul

build:
	dotnet build

move:
	move $(BUILD_DIR)\$(PLUGIN_NAME).dll $(PLUGIN_DIR)\
