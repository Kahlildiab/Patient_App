 $(document).ready(function () {
     
          if ($(window).width() < 576) {
           $(document).scroll(function () {
                if ($(this).scrollTop() > 0) {
                    $("#logo").css({ "background": "#DAA328","padding-right":"10px" });
                    $("#logo img").css({ "max-width": "50px","margin-top":"0px" });
                    $("#logo h1").css({ "margin-top": "10px","margin-left": "10px","font-size":"14px"});
                    $("#First").css({ "background": "#252468" });
                    $("#Second").css({ "background": "#264C9F" });
                    $("#opacity").css({ "position": "sticky","opacity": "1","padding-top":"0px", "background": "#252468"});
                    $("#vl").css({ "display": "none"});
            
                    $("#langarea").css({ "background": "#DAA328" });
                   $("#lang").css({ "margin-left": "5px","margin-right": "5px","top": "24px","font-size":"14px" });
                    $(".search").css({ "background": "#daa328","padding-top": "32px","padding-bottom": "32px" ,"top" : "0px", "height": "100%" });
                    $(".mobbar").css({ "top": "15px" });
                } else {
                    $("#logo").css({ "background": "#DAA328","padding-right":"10px" });
                    $("#logo img").css({ "max-width": "50px","margin-top":"0px" });
                    $("#logo h1").css({ "margin-top": "10px","margin-left": "10px","font-size":"14px"});
                    $("#First").css({ "background": "#252468" });
                    $("#Second").css({ "background": "#264C9F" });
                    $("#opacity").css({ "position": "sticky","opacity": "1","padding-top":"0px", "background": "#252468"});
                    $("#vl").css({ "display": "none"});
            
                    $("#langarea").css({ "background": "#DAA328" });
                        $("#lang").css({ "margin-left": "5px","margin-right": "5px","top": "24px","font-size":"14px" });
                    $(".search").css({ "background": "#daa328","padding-top": "32px","padding-bottom": "32px" ,"top" : "0px", "height": "100%" });
                    $(".mobbar").css({ "top": "15px" });
          
                }
            });
}
     
     
     
   else  if ($(window).width() < 1200  ) {
           $(document).scroll(function () {
                if ($(this).scrollTop() > 0) {
                    $("#logo").css({ "background": "#DAA328","padding-right":"10px" });
                    $("#logo img").css({ "max-width": "40px","margin-top":"5px" });
                    $("#logo h1").css({ "margin-top": "10px","margin-left": "10px","font-size":"14px"});
                    $("#First").css({ "background": "#252468" });
                    $("#Second").css({ "background": "#264C9F" });
                    $("#opacity").css({ "position": "sticky","opacity": "1","padding-top":"0px", "background": "#252468"});
                    $("#vl").css({ "display": "none"});
          
                    $("#langarea").css({ "background": "#DAA328" });
                    $("#lang").css({ "margin-left": "20px","margin-right": "20px","top": "7px","font-size":"14px" });
                    $(".search").css({ "background": "#daa328","padding-top": "32px","padding-bottom": "32px" ,"top" : "0px", "height": "100%" });
                    $(".mobbar").css({ "top": "15px" });
                } else {
                    $("#logo").css({ "background": "none","padding-right":"0px" });
                    $("#logo img").css({ "max-width": "100px","margin-top":"0px" });
                    $("#logo h1").css({ "margin-top": "20px","margin-left": "20px","font-size":"20px"});
                    $("#First").css({ "background": "#252468" });
                    $("#Second").css({"background": "#264C9F"  });
                    $("#opacity").css({ "position": "absolute","opacity": "1","padding-top":"20px", "background": "linear-gradient(360deg, rgba(0,0,38,0) 0%, rgba(0,0,38,0.80) 60%, rgba(0,0,38,0.95) 80%)"  });
                    $("#vl").css({ "display": "block"});
                    $("#langarea").css({ "background": "none" });
                    $("#lang").css({ "margin-left": "10px","margin-right": "10px","top": "45px","font-size":"16px" });
                    $(".search").css({ "background": "#264c9f","padding-top": "32px","padding-bottom": "32px","top" : "20px", "height": "auto" });
                    $(".mobbar").css({ "top": "40px" });
                
                }
            });
}
else {
     $(document).scroll(function () {
                if ($(this).scrollTop() > 700) {
                
                    $("#logo").css({ "background": "#DAA328"  });
                    $("#logo img").css({ "max-width": "60px","margin-top":"5px" });
                    $("#logo h5").css({ "margin-top": "20px","margin-left": "10px"});
                    $("#First").css({ "background": "#252468" });
                    $("#Second").css({ "background": "#264C9F" });
                    $("#opacity").css({ "position": "fixed","opacity": "1","padding-top":"0px" ,"transition" : "all 0.1s"   });
                    $("#vl").css({ "display": "none"});
           
                    $("#langarea").css({ "background": "#DAA328" });
                    $("#lang").css({ "margin-left": "20px","margin-right": "20px","top": "7px" });
                    $(".search").css({ "background": "#daa328","padding-top": "32px","padding-bottom": "32px" ,"top" : "0px", "height": "100%" });
                    $(".navbar").css({ "padding-bottom": "0px","-webkit-box-shadow":"0px 8px 9px 0px rgba(37,36,104,0.9)"     });
                } 
                
                
        
                
              else if ($(this).scrollTop() > 0) {
                
                    $("#logo").css({ "background": "none" });
                    $("#logo img").css({ "max-width": "120px" ,"margin-top":"0px" });
                    $("#logo h5").css({ "margin-top": "20px","margin-left": "20px"});
                    $("#First").css({ "background": "none" });
                    $("#Second").css({ "background": "none" });
                    $("#opacity").css({ "position": "fixed","opacity": "1","padding-top":"20px" });
              
                    $("#vl").css({ "display": "block"});
              
                    $("#langarea").css({ "background": "none" });
                    $("#lang").css({ "margin-left": "10px","margin-right": "10px","top": "35px" });
                    $(".search").css({ "background": "#264c9f","padding-top": "32px","padding-bottom": "32px","top" : "20px", "height": "auto"});
                    $(".navbar").css({ "padding-bottom": "20px","padding-top": "20px","-webkit-box-shadow":"0px 8px 9px 0px rgba(37,36,104,0)" });
                } 
                
                
     
                
                
                else {
                    $("#logo").css({ "background": "none" });
                    $("#logo img").css({ "max-width": "120px" ,"margin-top":"0px" });
                    $("#logo h5").css({ "margin-top": "20px","margin-left": "20px"});
                    $("#First").css({ "background": "none" });
                    $("#Second").css({ "background": "none" });
                    $("#opacity").css({ "position": "absolute","opacity": "1","padding-top":"20px"  });
              
                    $("#vl").css({ "display": "block"});
              
                    $("#langarea").css({ "background": "none" });
                    $("#lang").css({ "margin-left": "10px","margin-right": "10px","top": "35px" });
                    $(".search").css({ "background": "#264c9f","padding-top": "32px","padding-bottom": "32px","top" : "20px", "height": "auto" });
                    $(".navbar").css({ "padding-bottom": "60px","padding-top": "20px","-webkit-box-shadow":"0px 8px 9px 0px rgba(37,36,104,0)" });

                }
            });
            $("#logo").height($(".collapse").height());
           $("#langarea").height($("#logo").height()); 
new WOW().init();
            
}

 


        });
        

 
