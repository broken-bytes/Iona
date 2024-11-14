"use strict";(self.webpackChunkIona_lang=self.webpackChunkIona_lang||[]).push([[89],{8906:(e,n,s)=>{s.r(n),s.d(n,{assets:()=>l,contentTitle:()=>r,default:()=>p,frontMatter:()=>a,metadata:()=>c,toc:()=>o});var i=s(4848),t=s(8453);const a={sidebar_position:8,description:"Classes in Iona"},r="Classes",c={id:"guide/classes",title:"Classes",description:"Classes in Iona",source:"@site/docs/guide/classes.md",sourceDirName:"guide",slug:"/guide/classes",permalink:"/docs/guide/classes",draft:!1,unlisted:!1,editUrl:"https://github.com/broken-bytes/iona-lang/docs/guide/classes.md",tags:[],version:"current",sidebarPosition:8,frontMatter:{sidebar_position:8,description:"Classes in Iona"},sidebar:"tutorialSidebar",previous:{title:"Structures",permalink:"/docs/guide/structs"},next:{title:"Contracts",permalink:"/docs/guide/contracts"}},l={},o=[{value:"Initialisation",id:"initialisation",level:2},{value:"Inheritance",id:"inheritance",level:2}];function d(e){const n={admonition:"admonition",code:"code",em:"em",h1:"h1",h2:"h2",p:"p",pre:"pre",...(0,t.R)(),...e.components};return(0,i.jsxs)(i.Fragment,{children:[(0,i.jsx)(n.h1,{id:"classes",children:"Classes"}),"\n",(0,i.jsx)(n.p,{children:"Just like Structs, classes are reusable bits of code that contain data and provide functionality in a scoped and secure way.\nUnlike structs, which are value types, classes in Iona are always reference types."}),"\n",(0,i.jsx)(n.p,{children:"A struct is defined like this:"}),"\n",(0,i.jsx)(n.pre,{children:(0,i.jsx)(n.code,{className:"language-Iona",children:"class User {\n    ...\n}\n"})}),"\n",(0,i.jsx)(n.p,{children:"Classes may contain properties (variables) and functions (both reading and mutating):"}),"\n",(0,i.jsx)(n.pre,{children:(0,i.jsx)(n.code,{className:"language-Iona",children:"class Ticket {\n    let price = 22.99\n    let seat = 3\n    let row = 23\n}\n\nclass TicketServer {\n    var leftTickets: [Ticket] = [Ticket { price: 22.99, seat: 2, row: 23 }]\n\n    mut fn buyTicket(row: Int, seat: Int) {\n        // Find ticket and remove it from the array ...\n        self.leftTickets.remove(...)\n    }\n\n    fn availableTickets() -> [Ticket]& {\n        return self.leftTickets\n    }\n}\n"})}),"\n",(0,i.jsxs)(n.admonition,{type:"note",children:[(0,i.jsxs)(n.p,{children:["Functions inside classes are called ",(0,i.jsx)(n.em,{children:"methods"}),"."]}),(0,i.jsxs)(n.p,{children:["They automatically have access to ",(0,i.jsx)(n.code,{children:"self"}),". ",(0,i.jsx)(n.code,{children:"self"})," always points to the object itself."]})]}),"\n",(0,i.jsx)(n.h2,{id:"initialisation",children:"Initialisation"}),"\n",(0,i.jsxs)(n.p,{children:["Every class needs at least one init that assigns ",(0,i.jsx)(n.em,{children:"all"})," properties:"]}),"\n",(0,i.jsx)(n.pre,{children:(0,i.jsx)(n.code,{className:"language-Iona",children:"class Ticket {\n    let price: Float\n\n    init(price: Float) {\n        self.price = price\n    }\n}\n\nvar ticket = Ticket(price: 23.99)\n"})}),"\n",(0,i.jsx)(n.admonition,{type:"note",children:(0,i.jsx)(n.p,{children:"There is no auto-generated init for classes."})}),"\n",(0,i.jsxs)(n.p,{children:["One ",(0,i.jsx)(n.code,{children:"init"})," may call another ",(0,i.jsx)(n.code,{children:"init"}),", but there must be one ",(0,i.jsx)(n.code,{children:"default init"})," that all other ",(0,i.jsx)(n.code,{children:"init"}),"s call:"]}),"\n",(0,i.jsx)(n.pre,{children:(0,i.jsx)(n.code,{className:"language-Iona",children:"class Ticket {\n    let price: Float\n\n    default init(price: Float) {\n        self.price = price\n    }\n\n    init(price: Float, tax: Float) {\n        init(price: price - (price * tax))\n    }\n}\n\nvar ticket = Ticket(price: 23.99, tax: 0.1)\n"})}),"\n",(0,i.jsx)(n.p,{children:"When using init, every property must be initialised:"}),"\n",(0,i.jsx)(n.pre,{children:(0,i.jsx)(n.code,{className:"language-Iona",children:"class Ticket {\n    let price: Float\n    let seat: Int\n\n    init(price: Float) {\n        self.price = price\n        // Error: seat was not initialised in init\n    }\n}\n\nvar ticket = Ticket(price: 23.99)\n"})}),"\n",(0,i.jsx)(n.h2,{id:"inheritance",children:"Inheritance"}),"\n",(0,i.jsx)(n.p,{children:"Classes may either conform to a contract (implementing its properties and methods) or inherit from another class (subclassing)."}),"\n",(0,i.jsx)(n.p,{children:"Subclassing:"}),"\n",(0,i.jsx)(n.pre,{children:(0,i.jsx)(n.code,{className:"language-Iona",children:"class Device {\n    let storage: Float\n\n    init(storage: Float) {\n        self.storage = storage\n    }\n\n    fn readFrom(index: Int) {\n        ...\n    }\n}\n\nclass Phone: Device {\n    init {\n        super.init(storage: 512)\n    }\n}\n\n\nvar phone = Phone()\nphone.readFrom(index: 0)\n"})}),"\n",(0,i.jsx)(n.p,{children:"Conformance:"}),"\n",(0,i.jsx)(n.pre,{children:(0,i.jsx)(n.code,{className:"language-Iona",children:"contract Storage {\n    var storage: Int { get }\n\n    fn store(index: Int)\n}\n\nclass SolidStateDrive: Storage {\n    var storage: Int\n\n    init(size: Int) {\n        self.storage = size\n    }\n\n    fn store(index: Int) {\n        ...\n    }\n}\n"})})]})}function p(e={}){const{wrapper:n}={...(0,t.R)(),...e.components};return n?(0,i.jsx)(n,{...e,children:(0,i.jsx)(d,{...e})}):d(e)}},8453:(e,n,s)=>{s.d(n,{R:()=>r,x:()=>c});var i=s(6540);const t={},a=i.createContext(t);function r(e){const n=i.useContext(a);return i.useMemo((function(){return"function"==typeof e?e(n):{...n,...e}}),[n,e])}function c(e){let n;return n=e.disableParentContext?"function"==typeof e.components?e.components(t):e.components||t:r(e.components),i.createElement(a.Provider,{value:n},e.children)}}}]);