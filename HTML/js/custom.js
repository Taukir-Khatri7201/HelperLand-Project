$(document).ready(function() {
  $(document).scroll(function () {
    var $nav = $(".fixed-top");
    $nav.toggleClass('scrolled', $(this).scrollTop() > $nav.height());
    if($("nav:first").hasClass("scrolled") || $(window).width() <= 768) {
      document.getElementById("brand-img").setAttribute("src", "assets/logo-small.png");
    } else {
      document.getElementById("brand-img").setAttribute("src", "assets/logo-large.png");
    }
  });

  if($(window).width() <= 768) {
    document.getElementById("brand-img").setAttribute("src", "assets/logo-small.png");
  }
});