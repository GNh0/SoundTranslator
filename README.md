# SoundTranslator


Language : c#

Framework : .NET Core 8.0

Project : c# Console APP

사용 라이브러리 : DeePL.net, NAudio, Vosk
 
목적 : 출력되는 음성 사운드를 인식하여 텍스트로 변환하고 그 텍스트를 원하는 언어로 변역하기위해서

기능 :

NAudio를 사용하여 출력되는 사운드를 캡쳐

Vosk 모델을 사용하여 캡쳐한 사운드에서 음성을 텍스트로 추출

추출한 텍스트를 DeepL API를 사용하여 번역

추출한 음성 텍스트, 번역 텍스트를 파일로 저장
