(defun fac (n)
  (defun ifac (acc i)
    (if (= 0 i)
        acc
        (ifac (* acc i) (- i 1))))
  (ifac 1 n))
(fac 5)

--------------------------------------------------------------------------------

0000 10 90 00 00 00                 JMP label4

0005                              label2:
0005 0c 01 00 00 00 00 00 00 00     ENTER 1 fac
000e 03 01 00 00 00                 DEFINE n
0013 01                             POP
0014 10 6d 00 00 00                 JMP label3

0019                              label0:
0019 0c 03 00 00 00 02 00 00 00     ENTER 3 ifac
0022 03 02 00 00 00                 DEFINE ifac
0027 03 03 00 00 00                 DEFINE i
002c 03 04 00 00 00                 DEFINE acc
0031 01                             POP
0032 02 00 00 00 00                 PUSHINT 0
0037 04 03 00 00 00                 GET i
003c 05                             NUMEQUAL
003d 0b 48 00 00 00                 BFALSE label1
0042 04 04 00 00 00                 GET acc
0047 0d                             RET

0048                              label1:
0048 04 02 00 00 00                 GET ifac
004d 04 04 00 00 00                 GET acc
0052 04 03 00 00 00                 GET i
0057 08                             MUL
0058 04 03 00 00 00                 GET i
005d 02 01 00 00 00                 PUSHINT 1
0062 07                             SUB
0063 04 02 00 00 00                 GET ifac
0068 0f 03 00 00 00                 TAILCALL 3

006d                              label3:
006d 11 19 00 00 00                 GETLABEL label0
0072 03 02 00 00 00                 DEFINE ifac
0077 04 02 00 00 00                 GET ifac
007c 02 01 00 00 00                 PUSHINT 1
0081 04 01 00 00 00                 GET n
0086 04 02 00 00 00                 GET ifac
008b 0f 03 00 00 00                 TAILCALL 3

0090                              label4:
0090 11 05 00 00 00                 GETLABEL label2
0095 03 00 00 00 00                 DEFINE fac
009a 04 00 00 00 00                 GET fac
009f 02 05 00 00 00                 PUSHINT 5
00a4 0e 01 00 00 00                 CALL 1
00a9 00                             END

Symboltabelle:
00 fac
01 n
02 ifac
03 i
04 acc

--------------------------------------------------------------------------------

Dateiformat (binaer):
<Header><Block0><Block1><Block2>...<Footer>

Header:
- 4 Bytes, ASCII "LBVM"
- 1 Byte Versionsnummer
- 3 Bytes reserviert

Block:
- 1 Byte Blocktyp
- 4 Bytes Datenlaenge
- n Bytes Daten

Blocktypen:
- 00 Reserviert (spaeter Programminfo)
- 01 Code
- 02 Symboltabelle
- ff Footer

Datenbereich:
- Code: Bytecode, Offset im Datenbereich = IP
- Symboltabelle: Je Symbol 4 Bytes Nummer, 4 Bytes Laenge des Namens, Name in ASCII
- Footer: 1 Byte Pruefsumme 1 (ALLE Bytes bis auf den Footer addiert Modulo 256), 1 Byte Pruefsumme 2 (ALLE Bytes bis auf den Footer XOR-verknuepft)

--------------------------------------------------------------------------------

Aufbau Prototyp:

- Drei Stacks: Callstack (Paare von IP/number-of-arguments), Environment, Value-Stack
- Bei Programmstart enthaelt der Environment-Stack bereits ein Environment (global), die anderen Stacks sind leer
- Symboltabelle mit Zuordnung von Symbolnummer zu Name, nur fuer Debugging-Zwecke
- Erst mal nur Int32 und Bool

--------------------------------------------------------------------------------

Aufbau Bytecode:
- Little Endian
- Integer-Werte sind IMMER signed, 32 Bit

END
0x00
Programm wird beendet

POP
0x01
POPpt einen Wert vom Value-Stack und verwirft ihn

PUSHINT <number>
0x02 (number)
PUSHt einen konstanten Integer-Wert auf den Value-Stack

DEFINE <variable>
0x03 (Symbolnummer)
POPpt einen Wert vom Value-Stack, erzeugt eine neue Variable mit der angegebenen Symbolnummer im Environment-TOS und setzt die Variable auf den gePOPpten Wert

GET <variable>
0x04 (Symbolnummer)
Holt sich die Adresse der Variablen mit der angegebenen Symbolnummer im Environment-TOS, liest ihren Wert aus und PUSHt ihn auf den Value-Stack

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

GETLABEL <IP>
0x11 (number)
Wie PUSHINT, fuer Disassembler

IMOD
0x12
POPpt zwei Werte vom Value-Stack, konvertiert beide in Integer, dividiert TOS-1 durch TOS und PUSHt das Modulo-Ergebnis auf den Value-Stack

SET <variable>
0x13 (Symbolnummer)
POPpt einen Wert vom Value-Stack, holt sich die Adresse der Variablen mit der angegebenen Symbolnummer im Environment-TOS und setzt die Variable auf den gePOPpten Wert

ERROR
0xff
Programmfehler
