from pathlib import Path
from io import BytesIO
import html,re,json,shutil
from reportlab.pdfgen import canvas
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import Paragraph,Table,TableStyle
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib import colors
from reportlab.lib.utils import ImageReader
from PIL import Image,ImageDraw
import pymupdf
from month_report_content import PAGES

ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'output/submission'
FINAL=ROOT/'output/pdf/他山之石-作品介绍与技术报告-一月增强版.pdf';FINAL.parent.mkdir(parents=True,exist_ok=True)
CANON=BASE/'他山之石-李玉川-省赛交付/01_作品说明/他山之石-作品介绍与技术报告.pdf'
ARCHIVE=BASE/'report-archive';ARCHIVE.mkdir(exist_ok=True)
if CANON.exists() and not (ARCHIVE/'他山之石-原12页报告.pdf').exists():shutil.copy2(CANON,ARCHIVE/'他山之石-原12页报告.pdf')
for name,file in [('Body','MapSans.ttf'),('Heading','MapHeading.ttf')]:pdfmetrics.registerFont(TTFont(name,str(ROOT/'Game/Assets/Resources/Fonts'/file)))
W,H=595.276,841.89;LEFT=43;WIDTH=W-96
INK=colors.HexColor('#233c47');GOLD=colors.HexColor('#b79a63');PAPER=colors.HexColor('#faf9f6');MUTED=colors.HexColor('#627078')
BODY=ParagraphStyle('body',fontName='Body',fontSize=11.1,leading=18.1,wordWrap='CJK',textColor=INK)
SMALL=ParagraphStyle('small',parent=BODY,fontSize=8.4,leading=12.5,textColor=MUTED)
CELL=ParagraphStyle('cell',parent=BODY,fontSize=9.8,leading=14.4)
HEAD=ParagraphStyle('th',parent=CELL,fontName='Heading',textColor=colors.white)
CAPTION=ParagraphStyle('caption',parent=BODY,fontSize=8.6,leading=12.3,textColor=MUTED)
C=canvas.Canvas(str(FINAL),pagesize=(W,H));C.setTitle('他山之石｜作品介绍与技术报告｜一月开发增强版');C.setAuthor('李玉川 / 大连东软信息学院')
page_number=0;y=0;checks=[]
def safe(text):
    s=html.escape(str(text)).replace('\n','<br/>')
    return re.sub(r'https://[A-Za-z0-9./?=&;_%\-]+',lambda m:f'<link href="{m[0]}">{m[0]}</link>',s)
def drawp(text,style=BODY,width=WIDTH,x=LEFT,gap=9):
    global y
    p=Paragraph(safe(text),style);_,h=p.wrap(width,750)
    if y-h<84:raise RuntimeError(f'Page {page_number} overflow at paragraph: {str(text)[:35]} y={y} h={h}')
    p.drawOn(C,x,y-h);y-=h+gap
    return h
def base(title,kicker,refs):
    global page_number,y
    page_number+=1;C.setFillColor(PAPER);C.rect(0,0,W,H,fill=1,stroke=0)
    C.setFillColor(GOLD);C.rect(LEFT,H-43,65,3,fill=1,stroke=0)
    C.setFont('Body',8.6);C.setFillColor(MUTED);C.drawString(LEFT,H-65,kicker)
    C.setFont('Heading',21);C.setFillColor(INK);C.drawString(LEFT,H-101,title)
    C.bookmarkPage('s'+str(page_number));C.addOutlineEntry(title,'s'+str(page_number),0,False)
    C.setStrokeColor(colors.HexColor('#d8d6cd'));C.line(LEFT,72,W-LEFT,72)
    if refs:
        p=Paragraph(safe('依据：'+refs),SMALL);_,h=p.wrap(WIDTH,50);p.drawOn(C,LEFT,65-h)
    C.setFont('Body',7.9);C.setFillColor(MUTED);C.drawString(LEFT,22,'他山之石  |  李玉川 · 大连东软信息学院 · B组');C.drawRightString(W-LEFT,22,f'{page_number:02d} / 25')
    y=H-126
def finish():
    checks.append({'page':page_number,'body_end_y':round(y,2)});C.showPage()
def photo(path,caption='',maxheight=245,width=WIDTH,x=LEFT):
    global y
    file=ROOT/path
    if not file.exists():raise FileNotFoundError(file)
    im=Image.open(file).convert('RGB');im.thumbnail((1500,1100));buffer=BytesIO();im.save(buffer,format='JPEG',quality=88)
    h=min(width*im.height/im.width,maxheight);w=h*im.width/im.height
    if y-h<110:raise RuntimeError(f'Page {page_number} image overflow')
    C.drawImage(ImageReader(buffer),x+(width-w)/2,y-h,width=w,height=h);y-=h+7
    if caption:drawp(caption,CAPTION,width,x,gap=9)
def table(rows):
    global y
    n=len(rows[0]);widths=[WIDTH/n]*n
    if n==2:widths=[WIDTH*.25,WIDTH*.75]
    if n==3:widths=[WIDTH*.22,WIDTH*.57,WIDTH*.21]
    cells=[[Paragraph(safe(s),HEAD if i==0 else CELL) for s in row] for i,row in enumerate(rows)]
    t=Table(cells,colWidths=widths,hAlign='LEFT');t.setStyle(TableStyle([('BACKGROUND',(0,0),(-1,0),INK),('ROWBACKGROUNDS',(0,1),(-1,-1),[colors.HexColor('#efeee8'),colors.HexColor('#f6f5f0')]),('VALIGN',(0,0),(-1,-1),'TOP'),('LEFTPADDING',(0,0),(-1,-1),9),('RIGHTPADDING',(0,0),(-1,-1),9),('TOPPADDING',(0,0),(-1,-1),7),('BOTTOMPADDING',(0,0),(-1,-1),7),('LINEBELOW',(0,0),(-1,0),1,GOLD)]));_,h=t.wrap(WIDTH,700)
    if y-h<84:raise RuntimeError(f'Page {page_number} table overflow y={y} height={h}')
    t.drawOn(C,LEFT,y-h);y-=h+13
def flow(labels):
    global y
    # Two-column, numbered process boxes make the technical sequence scannable.
    cols=2;cw=(WIDTH-16)/2;start=y
    for i,text in enumerate(labels):
        xx=LEFT+(i%2)*(cw+16);yy=start-(i//2)*61
        C.setFillColor(colors.HexColor('#e7edf0'));C.roundRect(xx,yy-48,cw,48,6,fill=1,stroke=0)
        p=Paragraph(safe(f'{i+1:02d}  {text}'),CELL);_,h=p.wrap(cw-20,40);p.drawOn(C,xx+10,yy-12-h)
    y=start-((len(labels)+1)//2)*61-3
def gallery(items):
    global y
    cw=(WIDTH-14)/2;start=y
    for i,(path,caption) in enumerate(items):
        x=LEFT+(i%2)*(cw+14);yy=start-(i//2)*163
        file=ROOT/path;im=Image.open(file).convert('RGB');im.thumbnail((1000,700));buffer=BytesIO();im.save(buffer,format='JPEG',quality=88)
        h=min(130,cw*im.height/im.width);w=h*im.width/im.height
        C.drawImage(ImageReader(buffer),x+(cw-w)/2,yy-h,width=w,height=h)
        p=Paragraph(safe(caption),CAPTION);_,ph=p.wrap(cw,40);p.drawOn(C,x,yy-h-5-ph)
    y=start-((len(items)+1)//2)*163
    if y<84:raise RuntimeError(f'Page {page_number} gallery overflow')

base('他山之石','STONES FROM ANOTHER HILL / MONTHLY DEVELOPMENT REPORT','2026-10-15至2026-11-17开发记录；当前源码及真实运行证据')
C.setFont('Heading',15);C.drawString(LEFT,y,'作品介绍与技术报告 · 一月开发增强版');y-=31
drawp('校园 × 恋爱 × 悬疑 × 奇幻｜第一人称探索与立绘叙事',BODY)
photo('Game/Assets/Resources/Title/JingyingDusk.png','女主主题标题图；角色身份与姿态方向由作者选择，AI辅助绘制。',maxheight=275)
drawp('参赛者：李玉川  /  大连东软信息学院  /  个人赛 B组',BODY)
drawp('从冻结Web原型、迁移Unity与程序化世界，到八章剧情、透明立绘、任务存档和可复现交付。报告说明实现过程，也保留被作者否定和撤回的方案。',BODY)
table([['编制范围','当前交付'],['约一个月的开发脉络','独立Windows游戏 / 当前Unity源码'],['八章主线、六个角色身份','13间室内 / 三组真实AI协作链'],['本人最终参赛提交','游戏未发表、未获奖；视频另交']])
finish()
for index,page in enumerate(PAGES,1):
    base(page['title'],page['kicker'],page.get('refs',''))
    if page.get('quote'):
        style=ParagraphStyle('quote',parent=BODY,fontName='Heading',textColor=colors.HexColor('#705931'))
        drawp('用户原话节选：'+page['quote'],style,gap=13)
    for text in page.get('text',[]):drawp(text)
    if 'table' in page:table(page['table'])
    if 'flow' in page:flow(page['flow'])
    for text in page.get('text2',[]):drawp(text)
    if 'image' in page:
        room=y-121
        photo(page['image'],page.get('caption',''),maxheight=min(260,room))
    if 'gallery' in page:gallery(page['gallery'])
    finish()
C.save();assert page_number==25
doc=pymupdf.open(FINAL);assert len(doc)<=30 and FINAL.stat().st_size<100*1024*1024
review=BASE/'month-report-review';review.mkdir(exist_ok=True)
for i,page in enumerate(doc):page.get_pixmap(matrix=pymupdf.Matrix(1.25,1.25)).save(review/f'{i+1:02}.png')
for start in range(0,len(doc),9):
    contact=Image.new('RGB',(900,435*((min(9,len(doc)-start)+2)//3)),'#cbd0d1')
    for j in range(start,min(start+9,len(doc))):
        im=Image.open(review/f'{j+1:02}.png').convert('RGB');im.thumbnail((294,416));contact.paste(im,((j-start)%3*300,(j-start)//3*435))
    contact.save(review/f'contact-{start//9+1}.jpg')
(review/'layout-checks.json').write_text(json.dumps(checks,indent=2),encoding='utf-8')
shutil.copy2(FINAL,CANON)
print('Monthly report',len(doc),'pages',FINAL.stat().st_size,'bytes')
print('Minimum body baseline',min(x['body_end_y'] for x in checks))
