SOURCES = $(wildcard *.cs) $(wildcard */*.cs)

GUI_DIFF=bin/Release/native/gui-diff

$(GUI_DIFF): $(SOURCES) Makefile
	@dotnet publish /bl /nologo /v:diag

all: $(GUI_DIFF)

install: gui-diff $(GUI_DIFF)
	@echo "[INSTALL] gui-diff"
	@sed 's@%EXECUTABLE%@$(CURDIR)/$(GUI_DIFF)@' gui-diff > ~/bin/gui-diff
