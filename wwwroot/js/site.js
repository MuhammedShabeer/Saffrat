// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
"use strict";

// Preloader
$(window).on("load", function () {
    $(".loader").fadeOut("slow");
});

function GetCurrency(amount) {
    if (currencyformat.position == "0") {
        return currencyformat.symbol.toString() + amount.toString();
    }
    else if (currencyformat.position == "1") {
        return amount.toString() + currencyformat.symbol.toString();
    }
    else if (currencyformat.position == "2") {
        return currencyformat.symbol.toString() + ' ' + amount.toString();
    }
    else if (currencyformat.position == "3") {
        return amount.toString() + ' ' + currencyformat.symbol.toString();
    }
    return amount.toString();
}

function StrToNum(str) {
    var num = str.replace(/[^0-9]/g, '');
    return parseInt(num, 10);
}

function getNumber(num) {
    var res = num.toString().replace(/(?!-)[^0-9]/g, numberformat.decimalseparator);

    return res.toString();
}

function getLocaleNumber(num) {
    if (num === null || num === undefined || num === '') return 0;
    // Remove everything EXCEPT digits, dots, and minus signs.
    var res = num.toString().trim().replace(/(?!-)[^0-9.]/g, ''); 
    
    // If there are multiple dots (e.g. 1.234.56 from thousand separator replacement),
    // keep only the last one as the decimal separator.
    if (res.split('.').length > 2) {
        let parts = res.split('.');
        let last = parts.pop();
        res = parts.join('') + '.' + last;
    }
    
    return parseFloat(res) || 0;
}

/* Toaster Message */
function successMessage(msg) {
    iziToast.success({
        title: lang.success,
        message: msg,
        position: isRTL() ? 'topLeft' : 'topRight'
    });
}

function errorMessage(msg) {
    iziToast.error({
        title: lang.error,
        message: msg,
        position: isRTL() ? 'topLeft' : 'topRight'
    });
}

function performValidation(form) {
    var a = false;
    if (form[0].checkValidity() === false) {
        a = false;
    }
    else {
        a = true;
    }
    form.addClass("was-validated");

    return a;
}

function setCookie(cname, cvalue) {
    const d = new Date();
    if (cname == 'nightmode') {
        d.setTime(d.getTime() + (86400000));
    }
    else {
        d.setTime(d.getTime() + (3000));
    }
    let expires = "expires=" + d.toUTCString();
    document.cookie = cname + "=" + cvalue + ";" + expires + ";path=/";
}

function getCookie(cname) {
    let name = cname + "=";
    let ca = document.cookie.split(';');
    for (let i = 0; i < ca.length; i++) {
        let c = ca[i];
        while (c.charAt(0) == ' ') {
            c = c.substring(1);
        }
        if (c.indexOf(name) == 0) {
            return c.substring(name.length, c.length);
        }
    }
    return "";
}
var deleteCookie = function (cname) {
    document.cookie = cname + '=;expires=Thu, 01 Jan 1970 00:00:01 GMT;';
};

const formatAMPM = (date) => {
    let hours = date.getHours();
    let minutes = date.getMinutes();
    const ampm = hours >= 12 ? 'pm' : 'am';
    hours %= 12;
    hours = hours || 12;
    minutes = minutes < 10 ? `0${minutes}` : minutes;
    const strTime = `${hours}:${minutes} ${ampm}`;
    return strTime;
};

const getCurrentDate = (t) => {
    const date = ('0' + t.getDate()).slice(-2);
    const month = ('0' + (t.getMonth() + 1)).slice(-2);
    const year = t.getFullYear();
    return `${date}/${month}/${year}`;
};
function getDate(date) {
    var d = new Date(date);
    const offset = d.getTimezoneOffset();
    d = new Date(d.getTime() - (offset * 60 * 1000));
    return d.toISOString().split('T')[0];
}

let msg = getCookie("successMessage");
if (msg != "") {
    successMessage(msg);
}

function InitCleaveNumber(a) {
    $(a).attr('placeholder', '0' + numberformat.decimalseparator + '00');
    if ($(a).length) {
        new Cleave(a, {
            numeral: true,
            numeralDecimalMark: numberformat.decimalseparator,
            delimiter: ''
        });
    }
}

function isRTL() {
    if ($("html [dir='rtl']").length) {
        return true;
    }
    return false;
}

// Global
$(function () {

    if ($('.cleave-number').length) {
        $('.cleave-number').attr('placeholder', '0' + numberformat.decimalseparator + '00');

        document.querySelectorAll('.cleave-number').forEach(function (el) {
            new Cleave(el, {
                numeral: true,
                numeralDecimalMark: numberformat.decimalseparator,
                delimiter: ''
            });
        });
    }

    if ($('.selectpicker').length) {
        $('.selectpicker').selectpicker({
            noneResultsText: lang.noresult
        });

        $(".bootstrap-select").each(function () {
            let me = $(this);

            let element = me.parent().find('.invalid-feedback');
            if (element.length) {
                me.append(element);
            }
        });
    }

    //Show language picker modal
    $('#switchLanguage').on('click', function (e) {
        $('#LanguageModal').modal('show');
    });

    //Dark/Light Mode
    $('#switchTheme').on('click', function (e) {
        let thememode = localStorage.getItem("thememode");
        if (thememode == 'dark') {
            localStorage.setItem("thememode", "light");
        } else {
            localStorage.setItem("thememode", "dark");
        }
        load_theme_setting();
    });

    var load_theme_setting = function () {
        let thememode = localStorage.getItem("thememode");
        if (thememode == 'dark') {
            $('body').addClass('dark');
            $('body').removeClass('light');
        }
        else {
            $('body').addClass('light');
            $('body').removeClass('dark');
        }
    };
    load_theme_setting();

    if (jQuery().summernote) {
        $(".summernote").summernote({
            dialogsInBody: true,
            followingToolbar: false,
            minHeight: 250,
            fontSizes: ['8', '9', '10', '11', '12', '14', '16', '18', '20', '22', '24', '28', '36', '48', '72'],
            toolbar: [
                ["style", ["bold", "italic", "underline", "clear"]],
                ["font", ["strikethrough"]],
                ['fontsize', ['fontsize']],
                ['fontname', ['fontname']],
                ['color', ['color']],
                ['para', ['ul', 'ol', 'paragraph']],
                ['height', ['height']],
                ['insert', ['picture', 'myvideo', 'link', 'table', 'hr']],
                ['view', ['codeview', 'help']]
            ]
        });
    }

    //main-content minimum height
    $(".main-content").css({
        minHeight: $(window).outerHeight() - 95
    });

    var sidebar_dropdown = function () {
        if ($(".main-sidebar").length) {
            $(".main-sidebar .sidebar-menu li a.has-dropdown")
                .off("click")
                .on("click", function () {
                    var me = $(this);

                    me.parent()
                        .find("> .dropdown-menu")
                        .slideToggle(500, function () {
                            return false;
                        });
                    return false;
                });
        }
    };
    sidebar_dropdown();

    //toogle sidebar
    var toggle_sidebar_mini = function (mini) {
        let body = $("body");

        if (!mini) {
            body.removeClass("sidebar-mini");
            $(".main-sidebar .sidebar-menu > li > ul .dropdown-title").remove();
            $(".main-sidebar .sidebar-menu > li > a").removeAttr("title");
        } else {
            body.addClass("sidebar-mini");
            body.removeClass("sidebar-show");
            $(".main-sidebar .sidebar-menu > li").each(function () {
                let me = $(this);

                if (me.find("> .dropdown-menu").length) {
                    me.find("> .dropdown-menu").hide();
                    me.find("> .menu-toggle.toggled").toggleClass('toggled');
                    me.find("> .dropdown-menu").prepend(
                        '<li class="dropdown-title pt-3">' + me.find("> a").text() + "</li>"
                    );
                } else {
                    me.find("> a").attr("data-bs-toggle", "tooltip");
                    me.find("> a").attr("title", me.find("> a").text());
                    $("[data-bs-toggle='tooltip']").tooltip({
                        placement: "right"
                    });
                }
            });
        }
    };

    let selectedMenu = null;
    $('.menu-toggle').on('click', function (e) {
        var $this = $(this);
        $this.toggleClass('toggled');
    });

    $.each($('.main-sidebar .sidebar-menu li.active'), function (i, val) {
        var $activeAnchors = $(val).find('a:eq(0)');

        $activeAnchors.addClass('toggled');
        $activeAnchors.next().show();
    });

    $("[data-toggle='sidebar']").click(function () {
        var body = $("body"),
            w = $(window);

        if (w.outerWidth() <= 1024) {
            if (body.hasClass("sidebar-gone")) {
                body.removeClass("sidebar-gone");
                body.addClass("sidebar-show");
            } else {
                body.addClass("sidebar-gone");
                body.removeClass("sidebar-show");
            }
        } else {
            if (body.hasClass("sidebar-mini")) {
                toggle_sidebar_mini(false);
            } else {
                toggle_sidebar_mini(true);
            }
        }

        return false;
    });

    var toggleLayout = function () {
        var w = $(window);

        if (w.outerWidth() <= 1024) {
            if ($("body").hasClass("sidebar-mini")) {
                toggle_sidebar_mini(false);
            }

            $("body").addClass("sidebar-gone");
            $("body").removeClass("sidebar-mini sidebar-show");
            $("body")
                .off("click")
                .on("click", function (e) {
                    if (
                        $(e.target).hasClass("sidebar-show")
                    ) {
                        $("body").removeClass("sidebar-show");
                        $("body").addClass("sidebar-gone");
                    }
                });
        } else {
            $("body").removeClass("sidebar-gone sidebar-show");
        }
    };
    toggleLayout();
    $(window).resize(toggleLayout);

    // tooltip
    $("[data-toggle='tooltip']").tooltip();
    $('[data-toggle="tooltip"]').click(function () {
        $('[data-toggle="tooltip"]').tooltip("hide");
    });
    $('[data-bs-toggle="tooltip"]').click(function () {
        $('[data-toggle="tooltip"]').tooltip("hide");
    });

    // popover
    $('[data-toggle="popover"]').popover({
        container: "body"
    });

    // Dismiss function
    $("[data-dismiss]").each(function () {
        var me = $(this),
            target = me.data("dismiss");

        me.click(function () {
            $(target).fadeOut(function () {
                $(target).remove();
            });
            return false;
        });
    });

    // Collapsable
    $("[data-collapse]").each(function () {
        var me = $(this),
            target = me.data("collapse");

        me.click(function () {
            $(target).collapse("toggle");
            $(target).on("shown.bs.collapse", function () {
                me.html('<i class="fas fa-minus"></i>');
            });
            $(target).on("hidden.bs.collapse", function () {
                me.html('<i class="fas fa-plus"></i>');
            });
            return false;
        });
    });

    // Background
    $("[data-background]").each(function () {
        var me = $(this);
        me.css({
            backgroundImage: "url(" + me.data("background") + ")"
        });
    });

    // Bootstrap 5 Validation
    $(".needs-validation").submit(function (e) {
        var form = $(this);
        if (form[0].checkValidity() === false) {
            e.preventDefault();
            e.stopPropagation();
        }
        form.addClass("was-validated");
    });

    // alert dismissible
    $(".alert-dismissible").each(function () {
        var me = $(this);

        me.find(".close").click(function () {
            me.alert("close");
        });
    });

    // Slide Toggle
    $("[data-toggle-slide]").click(function () {
        let target = $(this).data("toggle-slide");

        $(target).slideToggle();
        return false;
    });

    // Dismiss modal
    $("[data-dismiss=modal]").click(function () {
        $(this)
            .closest(".modal")
            .modal("hide");

        return false;
    });

    // Width attribute
    $("[data-width]").each(function () {
        $(this).css({
            width: $(this).data("width")
        });
    });

    // Height attribute
    $("[data-height]").each(function () {
        $(this).css({
            height: $(this).data("height")
        });
    });

    // full screen call
    $(document).on("click", ".fullscreen-btn", function (e) {
        if (
            !document.fullscreenElement && // alternative standard method
            !document.mozFullScreenElement &&
            !document.webkitFullscreenElement &&
            !document.msFullscreenElement
        ) {
            // current working methods
            if (document.documentElement.requestFullscreen) {
                document.documentElement.requestFullscreen();
            } else if (document.documentElement.msRequestFullscreen) {
                document.documentElement.msRequestFullscreen();
            } else if (document.documentElement.mozRequestFullScreen) {
                document.documentElement.mozRequestFullScreen();
            } else if (document.documentElement.webkitRequestFullscreen) {
                document.documentElement.webkitRequestFullscreen(
                    Element.ALLOW_KEYBOARD_INPUT
                );
            }
        } else {
            if (document.exitFullscreen) {
                document.exitFullscreen();
            } else if (document.msExitFullscreen) {
                document.msExitFullscreen();
            } else if (document.mozCancelFullScreen) {
                document.mozCancelFullScreen();
            } else if (document.webkitExitFullscreen) {
                document.webkitExitFullscreen();
            }
        }
    });
});
