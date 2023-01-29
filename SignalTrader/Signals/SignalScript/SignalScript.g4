grammar SignalScript;

signal: 'signal' LPAREN parameters=namedparamlist RPAREN (SEMI | signalblock) EOF;

signalblock: LBRACE account* RBRACE ;

account: name=ID LPAREN parameters=namedparamlist? RPAREN LBRACE funclist RBRACE ;

funclist: func* ;

func: name=ID LPAREN parameters=namedparamlist? RPAREN SEMI ; 

namedparamlist: namedparam (COMMA namedparam)* ;

namedparam: name=ID ASSIGN value=paramvalue ;

paramvalue: STRING      # stringParamValue
    | boolean           # booleanParamValue
    | side              # sideParamValue
    | direction         # directionParamValue
    | price             # priceParamValue
    | order             # orderParamValue
    | leverage          # leverageParamValue
    | ID                # identifierParamValue
    | FLOAT             # floatParamValue
    | FLOATP            # floatPercentParamValue
    | INT               # intParamValue
    | INTP              # intPercentParamValue
    ;

boolean: TRUE           # trueValue 
    | FALSE             # falseValue
    ;
    
side: BUY               # buySide
    | SELL              # sellSide
    ;
    
direction: LONG         # longDirection
    | SHORT             # shortDirection
    ;
    
price: BID              # bidPrice
    | ASK               # askPrice
    | LAST              # lastPrice
    ;
    
order: MARKET           # marketOrder
    | LIMIT             # limitOrder
    ;

leverage: CROSS         # crossLeverage
    | ISOLATED          # isolatedLeverage
    ;




WS       : [ \t\r\n]+ -> skip ;      // Skip whitespace
COMMENT  : '#'.*?NEWLINE -> skip;    // Skip comments

STRING   : '"'~('"')*'"' ;           // Match string

TRUE     : 'true' ;
FALSE    : 'false' ;
SHORT    : 'short' ;
LONG     : 'long' ;
BUY      : 'buy' ;
SELL     : 'sell' ;
BID      : 'bid' ;
ASK      : 'ask' ;
LAST     : 'last' ;
MARKET   : 'market' ;
LIMIT    : 'limit' ;
CROSS    : 'cross' ;
ISOLATED : 'isolated' ;

ID       : ALPHA (ALNUM | '_')* ;     // Match identifier

FLOAT    : '-'? DIGIT+ '.' DIGIT*     // Match floating-point number
    | '-'? '.' DIGIT+
    ;
FLOATP   : FLOAT PERCENT ;            // Match floating-point percent
INT      : '-'? DIGIT+ ;              // Match integer
INTP     : INT PERCENT ;              // Match integer percent

ASSIGN   : '=' ;
PERCENT  : '%' ;
LPAREN   : '(' ;
RPAREN   : ')' ;
LBRACE   : '{' ;
RBRACE   : '}' ;
SEMI     : ';' ;
COMMA    : ',' ;
NEWLINE  : '\r'?'\n' ;

fragment ALNUM : (ALPHA | DIGIT) ;
fragment ALPHA : (UPPER | LOWER) ;
fragment LOWER : [a-z] ;
fragment UPPER : [A-Z] ;
fragment DIGIT : [0-9] ;
