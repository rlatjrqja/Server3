# 목표 : 라이브러리가 라이브러리를 참조하지 않을 것
** 이를 위해 메인 프로젝트에서만 참조를 하게 만들어야 함. **
* 변경점 : socket에서 opcode 100 판단하지 말고 바로 handle로 발전 시킬 것.
---
1. 프로토콜 라이브러리 (헤더를 만들고 해석하는 역할)
- Const 변수도 여기다가 선언 하면 될 것 같다. C#도 enum이 있나?

2. Plain Text 송수신 프로토콜
- 라이브러리 안에서 가공 후 byte 배열을 반환할지 핸들 안에서 처리할지 고민 해봐야 할 것 같음
- 지금까지 생각으로는 아무래도 반환하는 쪽이 낫지 않나 싶긴 함...

3. File 송수신 프로토콜
- 고민은 Text와 똑같음. byte를 반환하는게 추후 암호화 라이브러리 붙이는 쪽에서도 유리 하지않나.

_Task는 어떻게 써야 적절하게 쓸 수 있는가...._
