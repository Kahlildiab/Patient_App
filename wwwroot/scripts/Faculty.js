 $(document).ready(function () {
     
          if ($(window).width() < 576) {
                     $("#logo").css({ "background": "#daa328","padding-right":"10px" });
                    $("#logo img").css({ "max-width": "50px","margin-top":"0px" });
                    $("#logo h1").css({ "margin-top": "10px","margin-left": "10px","font-size":"14px"});
                    $("#First").css({ "background": "#252468;"  });
                    $("#Second").css({ "background": "#264c9f" });
                    $("#opacity").css({ "position": "relative","opacity": "1","padding-top":"0px", "background": "#252468"});
                    $("#vl").css({ "display": "none"});
            
                    $("#langarea").css({ "background": "#daa328" });
                   $("#lang").css({ "margin-left": "20px","margin-right": "20px","top": "7px","font-size":"14px" });
                    $(".search").css({ "padding-top": "10px","padding-bottom": "10px" ,"top" : "0px","height": "100%" });
                    $(".mobbar").css({ "top": "15px" });
                    
                    
                    
           $(document).scroll(function () {
                if ($(this).scrollTop() > 0) {
                    $("#logo").css({ "background": "#daa328","padding-right":"10px" });
                    $("#logo img").css({ "max-width": "50px","margin-top":"0px" });
                    $("#logo h1").css({ "margin-top": "10px","margin-left": "10px","font-size":"14px"});
                    $("#First").css({ "background": "#252468;" });
                    $("#Second").css({ "background": "#264c9f" });
                    $("#opacity").css({ "position": "sticky","opacity": "1","padding-top":"0px", "background": "#252468"});
                    $("#vl").css({ "display": "none"});
            
                    $("#langarea").css({ "background": "#daa328" });
                   $("#lang").css({ "margin-left": "20px","margin-right": "20px","top": "7px","font-size":"14px" });
                    $(".search").css({ "padding-top": "32px","padding-bottom": "32px" ,"top" : "0px","height": "100%" });
                    $(".mobbar").css({ "top": "15px" });
                } else {
                    $("#logo").css({ "background": "#daa328","padding-right":"10px" });
                    $("#logo img").css({ "max-width": "50px","margin-top":"0px" });
                    $("#logo h1").css({ "margin-top": "10px","margin-left": "10px","font-size":"14px"});
                    $("#First").css({ "background": "#252468;" });
                    $("#Second").css({ "background": "#264c9f" });
                    $("#opacity").css({ "position": "relative","opacity": "1","padding-top":"0px", "background": "#252468"});
                    $("#vl").css({ "display": "none"});
            
                    $("#langarea").css({ "background": "#daa328" });
                   $("#lang").css({ "margin-left": "20px","margin-right": "20px","top": "7px","font-size":"14px" });
                    $(".search").css({ "padding-top": "32px","padding-bottom": "32px" ,"top" : "0px","height": "100%" });
                    $(".mobbar").css({ "top": "15px" });
          
                }
            });
}
     
     
     
   else  if ($(window).width() < 1200  ) {
                           $("#logo").css({ "background": "#daa328","padding-right":"10px" });
                    $("#logo img").css({ "max-width": "50px","margin-top":"5px" });
                    $("#logo h1").css({ "margin-top": "10px","margin-left": "10px","font-size":"14px"});
              $("#First").css({ "background": "#252468;" });
                    $("#Second").css({ "background": "#264c9f" });
                    $("#opacity").css({ "position": "relative","opacity": "1","padding-top":"0px", "background": "#252468"});
                    $("#vl").css({ "display": "none"});
          
                    $("#langarea").css({ "background": "#daa328" });
                    $("#lang").css({ "margin-left": "20px","margin-right": "20px","top": "7px","font-size":"14px" });
                    $(".search").css({ "padding-top": "32px","padding-bottom": "32px" ,"top" : "0px","height": "100%" });
                    $(".mobbar").css({ "top": "15px" });
           $(document).scroll(function () {
                if ($(this).scrollTop() > 0) {
                    $("#logo").css({ "background": "#daa328","padding-right":"10px" });
                    $("#logo img").css({ "max-width": "50px","margin-top":"10px" });
                    $("#logo h1").css({ "margin-top": "10px","margin-left": "10px","font-size":"14px"});
                    $("#First").css({ "background": "#252468" });
                    $("#Second").css({ "background": "#264C9F" });
                    $("#opacity").css({ "position": "sticky","opacity": "1","padding-top":"0px", "background": "#252468"});
                    $("#vl").css({ "display": "none"});
          
                    $("#langarea").css({ "background": "#daa328" });
                    $("#lang").css({ "margin-left": "20px","margin-right": "20px","top": "7px","font-size":"14px" });
                    $(".search").css({ "padding-top": "32px","padding-bottom": "32px" ,"top" : "0px","height": "100%" });
                    $(".mobbar").css({ "top": "15px" });
                } else {
                    $("#logo").css({ "background": "#daa328","padding-right":"10px" });
                    $("#logo img").css({ "max-width": "50px","margin-top":"10px" });
                    $("#logo h1").css({ "margin-top": "10px","margin-left": "10px","font-size":"14px"});
                    $("#First").css({ "background": "#252468" });
                    $("#Second").css({ "background": "#264C9F" });
                    $("#opacity").css({ "position": "relative","opacity": "1","padding-top":"0px", "background": "#252468"});
                    $("#vl").css({ "display": "none"});
          
                    $("#langarea").css({ "background": "#daa328" });
                    $("#lang").css({ "margin-left": "20px","margin-right": "20px","top": "7px","font-size":"14px" });
                    $(".search").css({ "padding-top": "10px","padding-bottom": "10px" ,"top" : "0px","height": "100%" });
                    $(".mobbar").css({ "top": "15px" });
                
                }
            });
}
else {
     $(document).scroll(function () {
                if ($(this).scrollTop() > 700) {
                
                    $("#logo").css({ "background": "#daa328"  });
                    $("#logo img").css({ "max-width": "60px","margin-top":"5px" });
                    $("#logo h5").css({ "margin-top": "20px","margin-left": "10px"});
                    $("#First").css({ "background": "#252468" });
                    $("#Second").css({ "background": "#264C9F" });
                    $("#opacity").css({ "position": "sticky","opacity": "1","padding-top":"0px"  });
                    $("#vl").css({ "display": "none"});
           
                    $("#langarea").css({ "background": "#daa328" });
                    $("#lang").css({ "margin-left": "20px","margin-right": "20px","top": "7px" });
                    $(".search").css({ "padding-top": "32px","padding-bottom": "32px" ,"top" : "0px","height": "100%" });
                    $(".navbar").css({ "padding-bottom": "0px","-webkit-box-shadow":"0px 8px 9px 0px rgba(37,36,104,0.9)"     });
                } 
                
              else if ($(this).scrollTop() > 0) {
                
                   $("#logo").css({ "background": "#daa328"  });
                    $("#logo img").css({ "max-width": "60px","margin-top":"5px" });
                    $("#logo h5").css({ "margin-top": "20px","margin-left": "10px"});
                    $("#First").css({ "background": "#252468" });
                    $("#Second").css({ "background": "#264C9F" });
                    $("#opacity").css({ "position": "sticky","opacity": "1","padding-top":"0px"  });
                    $("#vl").css({ "display": "none"});
           
                    $("#langarea").css({ "background": "#daa328" });
                    $("#lang").css({ "margin-left": "20px","margin-right": "20px","top": "7px" });
                    $(".search").css({ "padding-top": "32px","padding-bottom": "32px" ,"top" : "0px","height": "100%" });
                    $(".navbar").css({ "padding-bottom": "0px","-webkit-box-shadow":"0px 8px 9px 0px rgba(37,36,104,0.9)"     });
                } 
                
                else {
                    $("#logo").css({ "background": "#daa328"  });
                    $("#logo img").css({ "max-width": "60px","margin-top":"5px" });
                    $("#logo h5").css({ "margin-top": "20px","margin-left": "10px"});
                    $("#First").css({ "background": "#252468" });
                    $("#Second").css({ "background": "#264C9F" });
                    $("#opacity").css({ "position": "relative","opacity": "1","padding-top":"0px"  });
                    $("#vl").css({ "display": "none"});
           
                    $("#langarea").css({ "background": "#daa328" });
                    $("#lang").css({ "margin-left": "20px","margin-right": "20px","top": "7px" });
                    $(".search").css({ "padding-top": "32px","padding-bottom": "32px" ,"top" : "0px","height": "100%" });
                    $(".navbar").css({ "padding-bottom": "0px","-webkit-box-shadow":"0px 8px 9px 0px rgba(37,36,104,0.9)"     });

                }
            });
            $("#logo").height($(".collapse").height());
           $("#langarea").height($("#logo").height()); 
             $("#First .nav-link").css({ "padding-top": "10px","padding-bottom": "0px"  });
           
new WOW().init();


 $("#logo").css({ "background": "#daa328"  });
                    $("#logo img").css({ "max-width": "60px","margin-top":"5px" });
                    $("#logo h5").css({ "margin-top": "20px","margin-left": "10px"});
              $("#First").css({ "background": "#252468" });
                    $("#Second").css({ "background": "#264C9F" });
                    $("#opacity").css({ "position": "relative","opacity": "1","padding-top":"0px"  });
                    $("#vl").css({ "display": "none"});
           
                    $("#langarea").css({ "background": "#daa328" });
                    $("#lang").css({ "margin-left": "20px","margin-right": "20px","top": "7px" });
                    $(".search").css({ "padding-top": "32px","padding-bottom": "32px" ,"top" : "0px","height": "100%" });
                    $(".navbar").css({ "padding-bottom": "0px","-webkit-box-shadow":"0px 8px 9px 0px rgba(37,36,104,0.9)"     });


            
}

            


        });
        

 

        