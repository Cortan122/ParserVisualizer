PROGS = $(patsubst %.bnf,%.exe,$(wildcard *.bnf))
RTFS = $(patsubst %.bnf,%.rtf,$(wildcard *.bnf))

all: $(PROGS) $(RTFS)

%.js: %.pegjs
	pegjs --trace $<

%.cli.js: %.js
	echo '#!/usr/bin/env node' > $@
	cat $< >> $@
	echo 'module.exports.DefaultTracer.prototype.trace = ()=>{};' >> $@
	echo 'console.log(module.exports.parse(require("fs").readFileSync(0,"utf8")));' >> $@
	chmod +x $@

%.rtf: %.bnf BNF.cli.js
	./BNF.cli.js < $< > $@

%.pegjs: %.bnf BNF2.cli.js
	./BNF2.cli.js < $< > $@

%.js.o: %.js
	cp $< temp.data
	echo -e '\0' >> temp.data
	ld.exe -r -b binary temp.data -o $@
	rm temp.data

%.exe: %.js.o duktape.o parser.c
	gcc.exe -o $@ $^ -lm
	strip.exe $@

duktape.o: duktape.c
	gcc.exe -std=c99 -c -o duktape.o duktape.c -lm

clean:
	rm -f *.js *.exe *.o temp.data $(PROGS) $(RTFS)

install:
	rm -f ../ParserLib/parsers/*
	cp *.exe *.rtf ../ParserLib/parsers

.PHONY: all clean
# .INTERMEDIATE: %.js
