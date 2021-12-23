$(document).ready(function() {
  $(document).scroll(function () {
    var $nav = $(".fixed-top");
    $navlinks = $("li.nav-item.btn-with-outline");

    $nav.toggleClass('scrolled', $(this).scrollTop() > $nav.height());
    if($("nav:first").hasClass("scrolled") || $(window).width() <= 768) {
      $navlinks.addClass("small-blue-btn");
      document.getElementById("brand-img").setAttribute("src", "assets/logo-small.png");
    } else {
      $navlinks.removeClass("small-blue-btn");
      document.getElementById("brand-img").setAttribute("src", "assets/logo-large.png");
    }
  });

  if($(window).width() <= 768) {
    document.getElementById("brand-img").setAttribute("src", "assets/logo-small.png");
  }
});