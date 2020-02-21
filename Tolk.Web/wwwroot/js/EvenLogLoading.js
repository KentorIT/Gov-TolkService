$(function () {
    $("body").on("show.bs.collapse", ".collapse.load-dynamically", function () {
        var $url = tolkBaseUrl + $(this).data("dynamic-load-url");
        var $child = $(this).find(".table-responsive");
        $.ajax({
            url: $url,
            type: 'POST',
            dataType: 'html',
            success: function (data) {
                $child.html(data);
            },
            error: function (jqXHR, textStatus, errorThrown) {
                $child.html("<p>Listan gick inte att ladda.</p>");
            }
        });
    });
});
