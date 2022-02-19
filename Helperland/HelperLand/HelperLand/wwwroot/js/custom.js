$(window).on('load', function () {
	$('.loader').addClass("d-none");
});

$(document).ready(function () {


	$(document).scroll(function () {
		var $nav = $(".fixed-top");
		if (!$nav.hasClass("admin-navbar") && !$nav.hasClass("secondary-navbar")) {
			$navlinks = $("li.nav-item.btn-with-outline");
	
			$nav.toggleClass('scrolled', $(this).scrollTop() > $nav.height());
			if($("nav:first").hasClass("scrolled") || $(window).width() <= 768) {
				$navlinks.addClass("small-blue-btn");
				if(document.getElementById("brand-img")) {
					document.getElementById("brand-img").setAttribute("src", "/assets/logo-small.png");
				}
			} else {
				$navlinks.removeClass("small-blue-btn");
				if(document.getElementById("brand-img")) {
					document.getElementById("brand-img").setAttribute("src", "/assets/logo-large.png");
				}
			}
		}
	});

	var $privacycontainer = $('.privacy-policy');
	var $privacypolicybtn = $('.ok-btn');
	if ($privacycontainer) {
		$privacypolicybtn.click(function () {
			$privacycontainer.hide();
		});
    }

	if($(window).width() <= 768) {
		if(document.getElementById("brand-img")) {
			document.getElementById("brand-img").setAttribute("src", "/assets/logo-small.png");
		}
	}

	$("#toggler-icon").click( function() {
        if($("#toggler-icon").hasClass("fa-bars")) {
            $("#toggler-icon").toggleClass("fa-bars fa-times-circle");
            $(".admin-left-nav, .admin-left-nav ul").css({
                "display": "block",
            });
            
        } else {
            turnoffleftnav();
        }
    });

   function turnoffleftnav() {
        addremove($("#toggler-icon"), "fa-bars", "fa-times-circle");
        $(".admin-left-nav, .admin-left-nav ul").css({
            "display": "none",
        });
   }

   function addremove(ele, classadd, classremove) {
	   	if(ele.hasClass(classremove)) {
			ele.removeClass(classremove);
	   	}
		if(!ele.hasClass(classadd)) {
			ele.addClass(classadd);
	   	}
   }

    $(window).resize(function () {
        var value = $(document).width() + 16;
        if(value > 1199) {
			var toggler = $("#toggler-icon");
            $(".admin-left-nav, .admin-left-nav ul").css({
                "display": "block",
            });
        } else {
			if($("#toggler-icon").hasClass("fa-bars")) {
				turnoffleftnav();
			}
		}
    });
});