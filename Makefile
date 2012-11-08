SOURCES = $(wildcard *.cs) $(wildcard */*.cs)
REFERENCES = -r:System.Windows.Forms.dll -r:System.Drawing.dll -r:System.Data.dll -r:System.Web.dll

gui-diff.exe: $(SOURCES) Makefile
	@xbuild
	@cp bin/Debug/gui-diff.exe bin/Debug/gui-diff.exe.mdb .

all: gui-diff.exe

install: gui-diff.exe
	@echo "[INSTALL] gui-diff"
	@cp gui-diff gui-diff.exe gui-diff.exe.mdb ~/bin/
	
	