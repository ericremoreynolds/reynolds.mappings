import re

placeholder = re.compile('\\$([^\\$]+)\\$')

INDENT = "\t"
CURRENT_INDENT = 0
SUBS = []

class File:
	def __init__(self):
		self.current_indent = 0
		self.out = None
		
	def write(self, x):
		self.out.write("\t" * self.current_indent + x + "\n")
		
	def indent(self):
		self.current_indent += 1
		
	def deindent(self):
		self.current_indent -= 1
		
f = File()
write = f.write
indent = f.indent
deindent = f.deindent


def myformat(text):
	global placeholder, SUBS
	while True:
		m = placeholder.search(text)
		if m is None:
			return text
		s = None
		for sub in SUBS:
			if m.group(1) in sub:
				s = sub[m.group(1)]
				break
		if s is None:
			raise Exception("Substitution '%s' not set." % m.groups(1))
		text = text[:m.start()] + s + text[m.end():]

class Snippet:
	last = None
	def __init__(self, text, postfix):
		if Snippet.last is not None:
			with Snippet.last:
				pass
		write("".join(text))
		Snippet.last = self
		self.postfix = postfix
		
	def __enter__(self):
		write("{")
		indent()
		Snippet.last = None
		
	def __exit__(self, a, b, c):
		if Snippet.last is not None:
			with Snippet.last:
				pass
		deindent()
		write("}" + self.postfix)
		
class Subs:
	def __init__(self, subs):
		self.subs = subs
		
	def __enter__(self):
		global SUBS
		SUBS = [self.subs] + SUBS
		
	def __exit__(self, a, b, c):
		global SUBS
		SUBS = SUBS[1:]
		
def stmt(text, postfix = ";"):
	write(myformat(text) + postfix)
	
def block(text, postfix=""):
	return Snippet(myformat(text), postfix)

def codegen_begin(fn):
	global f
	Snippet.last = None
	f.out = open(fn ,"w")
	
def codegen_end():
	f.out.flush()
	f.out = None
	
def placeholders(**subs):
	return Subs(subs)

def pascal_case(s):
	return "".join([x[0].upper() + x[1:] for x in s.split()])
	
CS_KEYWORDS = set(["base"])
def camel_case(s):
	s = pascal_case(s)
	s = s[0].lower() + s[1:]
	if s in CS_KEYWORDS:
		s = "@" + s;
	return s
				
__all__ = [ "codegen_begin", "codegen_end", "placeholders", "stmt", "block", "camel_case", "pascal_case" ]
