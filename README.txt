File format (binary):
<Header><Block0><Block1><Block2>...<Footer>

Header:
- 4 Bytes ASCII "LBVM"
- 1 Byte version number
- 3 Bytes reserved

Block:
- 1 Byte block type
- 4 Bytes data length
- n Bytes data

Block types:
- 00 Reserved (e.g. for program info)
- 01 Code
- 02 Symbol table
- ff Footer

Data:
- Code: Bytecode
- Symbol table: For each symbol: 4 bytes symbol number, 4 bytes length of name in bytes, name as ASCII, no 0x00 at the end
- Footer: Two checksums of 1 byte each: Take all bytes up until the start of the footer block, and 1. add them modulo 256, 2. XOR them together

--------------------------------------------------------------------------------

Runtime:

- Everything is little endian
- Integer values are signed, 32 bit
- Three stacks: Function calls, Environment, Values
- On program start the environment stack contains a (global) environment, the other stacks are empty
- Symbol table for assigning numbers to symbols (think Scheme symbols or Erlang atoms)

--------------------------------------------------------------------------------

Assembler directives:


blub:
Any symbol followed by a colon defines a label.


FUNCTION name parameters [&closingover closing-variables] [&localdefines localvariables]
Generate two new, unique labels, and expand to:
   JMP label0
 label1:
   ENTER (length parameters + length closing-variables) name
   DEFINE parameterN
   ...
   DEFINE parameter1
   DEFINE parameter0
   DEFINE closing-variableN
   ...
   DEFINE closing-variable1
   DEFINE closing-variable0
   NEWVAR localvariable0
   NEWVAR localvariable1
   ...
   NEWVAR localvariableN
   POP


ENDFUNCTION
Get data of the function being defined, and expand to
 label0:
   PUSHLABEL label1
   DEFINE name

If a closure was being defined, add the following:
  PUSHVAR name
  PUSHSYM closing-variable0
  PUSHSYM closing-variable1
  ...
  PUSHSYM closing-variableN
  MAKECLOSURE (length closing-variables)
  SET name

--------------------------------------------------------------------------------

Assembler opcodes:


END
0x00
Programm wird beendet. Veraendert keinen Stack.

POP
0x01
POPpt einen Wert vom Value-Stack und verwirft ihn.

PUSHINT <number>
0x02 (number)
PUSHt einen konstanten Integer-Wert auf den Value-Stack.

DEFINE <variable>
0x03 (Symbolnummer)
POPpt einen Wert vom Value-Stack, erzeugt eine neue Variable mit der angegebenen Symbolnummer im Environment-TOS und setzt die Variable auf den gePOPpten Wert.
Falls der gePOPpte Wert selbst eine Variable ist, wird lediglich eine Referenz auf diese Variable im Environment-TOS abgelegt, statt eine neue Variable zu erzeugen.
War bereits eine Variable mit der angegebenen Symbolnummer vorhanden UND hat den Wert UNASSIGNED, so wird sie weiterverwendet.

PUSHVAR <variable>
0x04 (Symbolnummer)
Holt sich die Adresse der Variablen mit der angegebenen Symbolnummer im Environment-TOS, liest ihren Wert aus und PUSHt ihn auf den Value-Stack.
Hat die Variable den Wert UNASSIGNED, so bricht das Programm mit einer Fehlermeldung ab.

NUMEQUAL
0x05
POPpt zwei Werte vom Value-Stack, vergleicht sie und PUSHt true (Werte sind gleich) oder false (Werte sind ungleich) auf den Value-Stack

ADD
0x06
POPpt zwei Werte vom Value-Stack, addiert sie und PUSHt das Ergebnis auf den Value-Stack

SUB
0x07
POPpt zwei Werte vom Value-Stack, subtrahiert TOS von TOS-1 und PUSHt das Ergebnis auf den Value-Stack

MUL
0x08
POPpt zwei Werte vom Value-Stack, multipliziert sie und PUSHt das Ergebnis auf den Value-Stack

DIV
0x09
POPpt zwei Werte vom Value-Stack, dividiert TOS-1 durch TOS und PUSHt das Ergebnis auf den Value-Stack

IDIV
0x0a
POPpt zwei Werte vom Value-Stack, konvertiert beide in Integer, dividiert TOS-1 durch TOS und PUSHt das Integer-Ergebnis auf den Value-Stack

BFALSE <label>
0x0b (absolute Sprungadresse)
POPpt einen Wert vom Value-Stack, und setzt IP auf die angegebene Sprungadresse, falls der Wert zu FALSE evaluiert

ENTER <number-of-parameters> <name>
0x0c (Parameteranzahl) (Symbolnummer)
Prueft, ob der letzte CALL- oder TAILCALL-Befehl mit der angegebenen Anzahl von Parametern ausgefuehrt wurde.
Falls nicht, wird ein Fehler mit dem angegebenen Symbol als Name des fehlerhaften Funktionsaufrufs erzeugt.
Ansonsten wird ein neues Environment erzeugt und auf den Environment-Stack gePUSHt.

RET
0x0d
POPpt den Environment-Stack und setzt IP auf POP(Call-Stack)

CALL <number-of-pushed-arguments>
0x0e (Parameteranzahl)
PUSHt IP und die Parameteranzahl auf den Call-Stack und setzt IP auf (TOS - number-of-pushed-arguments)

TAILCALL <number-of-pushed-arguments>
0x0f (Parameteranzahl)
Wie CALL, POPpt aber zuvor Environment-Stack und Call-Stack.

JMP <IP>
0x10 (IP)
Springt direkt an die angegebene Adresse.

PUSHLABEL <IP>
0x11 (number)
PUSHt den angegebenen IP auf den Value-Stack.

IMOD
0x12
POPpt zwei Werte vom Value-Stack, konvertiert beide in Integer, dividiert TOS-1 durch TOS und PUSHt das Modulo-Ergebnis auf den Value-Stack

SET <variable>
0x13 (Symbolnummer)
POPpt einen Wert vom Value-Stack, holt sich die Adresse der Variablen mit der angegebenen Symbolnummer im Environment-TOS und setzt die Variable auf den gePOPpten Wert.

PUSHSYM <variable>
0x14 (Symbolnummer)
PUSHt das angegebene Symbol auf den Value-Stack

PUSHTRUE
0x15
PUSHt den Wert TRUE auf den Value-Stack

PUSHFALSE
0x16
PUSHt den Wert FALSE auf den Value-Stack

MAKECLOSURE <number-of-pushed-arguments>
0x17 (Parameteranzahl)
Holt sich <number-of-pushed-arguments> Symbole vom Value-Stack, POPpt dann die IP eines Lambdas und gibt eine neu erzeugte Closure zurueck,
die wie ein Lambda aufgerufen wird, und dabei die an MAKECLOSURE uebergebenen VariablenREFERENZEN in der selben Reihenfolge PUSHT wie beim Aufruf von MAKECLOSURE.

NUMLT
0x18
POPpt zwei Werte vom Value-Stack, und PUSHt true, wenn TOS-1 < TOS, ansonsten false

NUMLE
0x19
POPpt zwei Werte vom Value-Stack, und PUSHt true, wenn TOS-1 <= TOS, ansonsten false

NUMGT
0x1a
POPpt zwei Werte vom Value-Stack, und PUSHt true, wenn TOS-1 > TOS, ansonsten false

NUMGE
0x1b
POPpt zwei Werte vom Value-Stack, und PUSHt true, wenn TOS-1 >= TOS, ansonsten false

PUSHDBL
0x1c (value as 8-byte-IEEE-float)
PUSHt eine Double-Konstante auf den Value-Stack

MAKEVAR <variable>
0x1d (Symbolnummer)
Erzeugt die Variable mit der angegebenen Symbolnummer im Environment-TOS und setzt sie auf den Wert UNASSIGNED.

MAKEPAIR
0x1e
POPpt zwei Werte vom Value-Stack und PUSHt das Paar (TOS-1, TOS).

ISPAIR
0x1f
POPpt einen Wert vom Value-Stack und PUSHt true, wenn der Wert ein Paar war, ansonsten false.

PAIR1
0x20
POPpt ein Paar vom Value-Stack und PUSHt dessen ersten Wert zurück.

PAIR2
0x21
POPpt ein Paar vom Value-Stack und PUSHt dessen zweiten Wert zurück.

PUSHNIL
0x22
PUSHt den speziellen Wert NIL auf den Value-Stack.

ENTERR <number-of-parameters> <number-of-parameters-to-skip> <name>
0x23 (Parameteranzahl) (Symbolnummer)
Analog ENTER, aber POPpt alle Werte, die "zuviel" an die Funktion uebergeben wurden, erzeugt daraus eine Liste,
und PUSHt diese als "letzten Parameter" auf den Stack. Die letzten <number-of-parameters-to-skip> Parameter werden
dabei uebersprungen, um weiterhin Closures verwenden zu koennen.

RANDOM
0x24
POPpt eine Zahl vom Value-Stack, konvertiert sie nach Int32, und PUSHt eine Zufallszahl zwischen 0 (einschliesslich)
und der gePOPpten Zahl (ausschliesslich) auf den Value-Stack.

ERROR
0xff
Programmfehler
