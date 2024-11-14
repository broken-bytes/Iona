"use strict";(self.webpackChunkIona_lang=self.webpackChunkIona_lang||[]).push([[246],{5208:(e,n,i)=>{i.r(n),i.d(n,{assets:()=>a,contentTitle:()=>o,default:()=>u,frontMatter:()=>r,metadata:()=>l,toc:()=>d});var t=i(4848),s=i(8453);const r={sidebar_position:2,description:"Builtin Types"},o="Builtin Types",l={id:"guide/builtins",title:"Builtin Types",description:"Builtin Types",source:"@site/docs/guide/builtins.md",sourceDirName:"guide",slug:"/guide/builtins",permalink:"/docs/guide/builtins",draft:!1,unlisted:!1,editUrl:"https://github.com/broken-bytes/iona-lang/docs/guide/builtins.md",tags:[],version:"current",sidebarPosition:2,frontMatter:{sidebar_position:2,description:"Builtin Types"},sidebar:"tutorialSidebar",previous:{title:"Variables, Mutables, Immutables",permalink:"/docs/guide/variables-mutables-immutables"},next:{title:"Comments",permalink:"/docs/guide/comments"}},a={},d=[{value:"Integer Types",id:"integer-types",level:2},{value:"Boolean",id:"boolean",level:2},{value:"Floating Point Numbers",id:"floating-point-numbers",level:2},{value:"Char",id:"char",level:2}];function c(e){const n={admonition:"admonition",code:"code",h1:"h1",h2:"h2",li:"li",p:"p",ul:"ul",...(0,s.R)(),...e.components};return(0,t.jsxs)(t.Fragment,{children:[(0,t.jsx)(n.h1,{id:"builtin-types",children:"Builtin Types"}),"\n",(0,t.jsx)(n.p,{children:"Iona comes with a number of builtin types. They are zero cost abstractions over the underlying raw types.\nThis means even though they provide additional features and syntax, they do not add to the runtime cost of applications."}),"\n",(0,t.jsx)(n.h2,{id:"integer-types",children:"Integer Types"}),"\n",(0,t.jsx)(n.p,{children:"Iona comes with different integer types, all with different bit-sizes, either signed or unsigned. An integer can only hold real numbers, no fractionals."}),"\n",(0,t.jsx)(n.p,{children:"These are the integer types available:"}),"\n",(0,t.jsxs)(n.ul,{children:["\n",(0,t.jsx)(n.li,{children:"NInt:    Platform-dependent signed integer, either 32 or 64 bit."}),"\n",(0,t.jsx)(n.li,{children:"NUInt:   Platform-dependent unsigned integer, either 32 or 64 bit."}),"\n",(0,t.jsx)(n.li,{children:"Int8:   8-bit signed integer"}),"\n",(0,t.jsx)(n.li,{children:"Int16:  16-bit signed integer"}),"\n",(0,t.jsx)(n.li,{children:"Int32:  32-bit signed integer"}),"\n",(0,t.jsx)(n.li,{children:"Int64:  64-bit signed integer"}),"\n",(0,t.jsx)(n.li,{children:"UInt8:  8-bit unsigned integer"}),"\n",(0,t.jsx)(n.li,{children:"UInt16: 16-bit unsigned integer"}),"\n",(0,t.jsx)(n.li,{children:"UInt32: 32-bit unsigned integer"}),"\n",(0,t.jsx)(n.li,{children:"UInt64: 64-bit unsigned integer"}),"\n"]}),"\n",(0,t.jsx)(n.p,{children:"Every integer comnes with bounds checking, resulting in an error when an overflow occurs.\nAdditionally, integers cannot be used in expressions together, unless converted to the right-sized integer.\nThere is no implicit integer promotion in Iona. Each integer is treated like a different type to ensure safety."}),"\n",(0,t.jsx)(n.h2,{id:"boolean",children:"Boolean"}),"\n",(0,t.jsxs)(n.p,{children:["Iona has a boolean type, holding ",(0,t.jsx)(n.code,{children:"true"})," or ",(0,t.jsx)(n.code,{children:"false"}),". A boolean is represented as a single byte in memory. While this wastes a few bits, it results in faster access times and less cache misses."]}),"\n",(0,t.jsx)(n.h2,{id:"floating-point-numbers",children:"Floating Point Numbers"}),"\n",(0,t.jsx)(n.p,{children:"Iona provides floating-point (or fractional) numbers that enable the storage of any rational number."}),"\n",(0,t.jsx)(n.p,{children:"There are two different types of floating point numbers in Iona:"}),"\n",(0,t.jsxs)(n.ul,{children:["\n",(0,t.jsx)(n.li,{children:"Float: 32-bit fractional"}),"\n",(0,t.jsx)(n.li,{children:"Double: 64-bit fractional"}),"\n"]}),"\n",(0,t.jsx)(n.p,{children:"Both types are signed and differ only in their precision (i.e., the number of bits used to represent the number)."}),"\n",(0,t.jsx)(n.h2,{id:"char",children:"Char"}),"\n",(0,t.jsxs)(n.p,{children:["Char is an alias for an ",(0,t.jsx)(n.code,{children:"Int8"})," used to represent that a value shall be used as a character and not a number. It has no functional difference."]}),"\n",(0,t.jsx)(n.admonition,{type:"note",children:(0,t.jsx)(n.p,{children:"Iona includes many more types in its standard library. The corresponding modules aren't imported by default, though."})})]})}function u(e={}){const{wrapper:n}={...(0,s.R)(),...e.components};return n?(0,t.jsx)(n,{...e,children:(0,t.jsx)(c,{...e})}):c(e)}},8453:(e,n,i)=>{i.d(n,{R:()=>o,x:()=>l});var t=i(6540);const s={},r=t.createContext(s);function o(e){const n=t.useContext(r);return t.useMemo((function(){return"function"==typeof e?e(n):{...n,...e}}),[n,e])}function l(e){let n;return n=e.disableParentContext?"function"==typeof e.components?e.components(s):e.components||s:o(e.components),t.createElement(r.Provider,{value:n},e.children)}}}]);