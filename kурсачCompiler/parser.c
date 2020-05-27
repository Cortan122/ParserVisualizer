#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#include <stdint.h>

#include "duktape.h"
#define BUF_SIZE 1024

char* ReadAllText(){
  char buffer[BUF_SIZE];
  size_t contentSize = 1; // includes NULL
  // Preallocate space. We could just allocate one char here, but that wouldn't be efficient.
  char* content = malloc(sizeof(char) * BUF_SIZE);
  if(content == NULL){
    perror("Failed to allocate content");
    exit(1);
  }
  content[0] = '\0'; // make null-terminated
  while(fgets(buffer, BUF_SIZE, stdin)){
    char* old = content;
    contentSize += strlen(buffer);
    content = realloc(content, contentSize);
    if(content == NULL){
      perror("Failed to reallocate content");
      free(old);
      exit(2);
    }
    strcat(content, buffer);
  }

  if(ferror(stdin)){
    free(content);
    perror("Error reading from stdin");
    exit(3);
  }

  return content;
}

extern char _binary_temp_data_start;
int argc;
char** argv;

duk_ret_t js_native_print(duk_context* ctx){
  puts(duk_to_string(ctx, 0));
  return 0;
}

duk_ret_t js_native_read(duk_context* ctx){
  if(argc == 2){
    duk_push_string(ctx, argv[1]);
  }else{
    duk_push_string(ctx, ReadAllText());
  }
  return 1;
}

void eval(duk_context* ctx, char* string){
  if(duk_peval_string(ctx, string) != 0){
    printf("eval failed: %s\n", duk_safe_to_string(ctx, -1));
    // exit(122);
  }
  duk_pop(ctx);
}

int main(int _argc, char** _argv){
  argc = _argc;
  argv = _argv;

  duk_context* jsState = duk_create_heap_default();

  duk_push_c_function(jsState, js_native_print, 1 /*nargs*/);
  duk_put_global_string(jsState, "echo");
  duk_push_c_function(jsState, js_native_read, 0 /*nargs*/);
  duk_put_global_string(jsState, "readFile");

  eval(jsState, "var console = {log: echo};");
  eval(jsState, "var module = {};");

  eval(jsState, &_binary_temp_data_start);
  eval(jsState, "module.exports.parse(readFile());");

  duk_destroy_heap(jsState);
}
