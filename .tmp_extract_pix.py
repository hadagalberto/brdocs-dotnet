import sys

text = open(r'C:\Users\hadag\AppData\Local\Temp\opencode\pix_manual.txt', encoding='utf-8').read()

query = sys.argv[1] if len(sys.argv) > 1 else 'Definições gerais em relação ao BR Code'
count = int(sys.argv[2]) if len(sys.argv) > 2 else 9000

i = -1
for _ in range(int(sys.argv[3]) if len(sys.argv) > 3 else 1):
    i = text.find(query, i + 1)
    if i < 0:
        break
if i < 0:
    print('NOT FOUND:', query)
else:
    print(text[i:i + count])
