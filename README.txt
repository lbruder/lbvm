(defun fac (n)
  (defun ifac (acc i)
    (if (= 0 i)
        acc
        (ifac (* acc i) (- i 1))))
  (ifac 1 n))
(fac 5)

--------------------------------------------------------------------------------

FUNCTION fac n

  FUNCTION ifac acc i &closingover ifac

    PUSHINT 0
    PUSHVAR i
    NUMEQUAL
    BFALSE label0
    PUSHVAR acc
    RET

  label0:
    PUSHVAR ifac
    PUSHVAR acc
    PUSHVAR i
    MUL
    PUSHVAR i
    PUSHINT 1
    SUB
    TAILCALL 2

  ENDFUNCTION

  PUSHVAR ifac
  PUSHINT 1
  PUSHVAR n
  TAILCALL 2

ENDFUNCTION

PUSHVAR fac
PUSHINT 5
CALL 1
END

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

- Little Endian
- Integer-Werte sind IMMER signed, 32 Bit
- Drei Stacks: Callstack (Paare von IP/number-of-arguments), Environment, Value-Stack
- Bei Programmstart enthaelt der Environment-Stack bereits ein Environment (global), die anderen Stacks sind leer
- Symboltabelle mit Zuordnung von Symbolnummer zu Name, nur fuer Debugging-Zwecke
- Erst mal nur Int32 und Bool

--------------------------------------------------------------------------------

Assembler-Direktiven:

labelx:
Definiert eine Sprungmarke

FUNCTION name parameters [&closingover closing-variables]
Pusht die Labels label0 und label1 auf einen "Funktions-Stack" im Assembler, expandiert zu
   JMP label0
 label1:
   ENTER (length parameters + length closing-variables) fac
   DEFINE parameterN
   ...
   DEFINE parameter1
   DEFINE parameter0
   DEFINE closing-variableN
   ...
   DEFINE closing-variable1
   DEFINE closing-variable0
   POP

ENDFUNCTION
Poppt die Labels label0 und label1 vom "Funktions-Stack" im Assembler, expandiert zu
 label0:
   PUSHLABEL label1
   DEFINE name

Falls zuvor eine Closure definiert wurde, wird zusaetzlich folgendes angehaengt:
  PUSHVAR name
  PUSHSYM closing-variable0
  PUSHSYM closing-variable1
  ...
  PUSHSYM closing-variableN
  MAKECLOSURE (length closing-variablen)
  SET name

--------------------------------------------------------------------------------

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

ERROR
0xff
Programmfehler
