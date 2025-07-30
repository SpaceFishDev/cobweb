src = $(wildcard src/*.c)
out = cbw
cflags = -O3

all: build run

build:
	gcc $(src) -o $(out) $(cflags)
run:
	./$(out)