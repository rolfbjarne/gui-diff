SOURCES = $(wildcard *.cs) $(wildcard */*.cs)

bin/Debug/gui-diff.exe: $(SOURCES) Makefile
	@msbuild

all: bin/Debug/gui-diff.exe

install: gui-diff bin/Debug/gui-diff.exe
	@echo "[INSTALL] gui-diff"
	@sed 's@%DIR%@$(CURDIR)@' gui-diff > ~/bin/gui-diff
