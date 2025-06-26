SOURCES = $(wildcard *.cs) $(wildcard */*.cs)

GUI_DIFF=bin/Release/native/gui-diff
GUI_DIFF_DEBUG=bin/Debug/gui-diff

$(GUI_DIFF): $(GUI_DIFF_DEBUG) $(SOURCES) Makefile $(wildcard *.csproj)
	@dotnet publish /bl:publish.binlog /nologo /v:diag

$(GUI_DIFF_DEBUG): $(SOURCES) Makefile $(wildcard *.csproj)
	@dotnet build /bl:build.binlog /nologo

all: $(GUI_DIFF) $(GUI_DIFF_DEBUG)

install: ~/bin/gui-diff ~/bin/gui-diff-debug

~/bin/gui-diff: gui-diff $(GUI_DIFF)
	@echo "[INSTALL] gui-diff"
	@sed 's@%EXECUTABLE%@$(CURDIR)/$(GUI_DIFF)@' gui-diff > ~/bin/gui-diff
	@chmod +x ~/bin/gui-diff

~/bin/gui-diff-debug: gui-diff $(GUI_DIFF_DEBUG)
	@echo "[INSTALL] gui-diff-debug"
	@sed 's@%EXECUTABLE%@$(CURDIR)/$(GUI_DIFF_DEBUG)@' gui-diff > ~/bin/gui-diff-debug
	@chmod +x ~/bin/gui-diff-debug
