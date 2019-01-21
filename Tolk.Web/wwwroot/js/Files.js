$(function () {
    var currentId = 0;
    $("body").on("click", ".remove-file-row", function () {
        var $tbody = $(this).closest("tbody");
        var $row = $(this).closest("tr");

        var $url = tolkBaseUrl + "Files/Delete?id=" + $row.data("id") + "&groupKey=" + $("#FileGroupKey").val();
        $.ajax({
            url: $url,
            type: 'DELETE',
            dataType: "json",
            success: function (data) {
                if (!data.success) {
                    alert(data.ErrorMessage, "Något gick fel när filen skulle tas bort!");
                }
            }
        });
        $row.remove();

        //Reindex 0 to n 
        //TODO: This might be possible to reuse, iso having this copy and the other copy on requirements removal in order creation.
        var $rows = $tbody.find("tr");
        currentId = 0;
        if ($rows.length === 0) {
            $tbody.closest('.file-list').addClass("d-none");
        } else {
            $rows.each(function () {
                $(this).find("input").each(function () {
                    var $id = $(this).prop("id").match(/\d+/);
                    $(this).prop("id", $(this).prop("id").replace($id, currentId));
                    $(this).prop("name", $(this).prop("name").replace($id, currentId));
                });
                currentId++;
            });
        }
    });
    $("body").on("click", ".file-table-view > tbody > tr > td:not(.table-button-column)", function () {
        var $row = $(this).closest("tr");
        window.open(tolkBaseUrl + "Files/Download?id=" + $row.data("id"));
    });

    $("body").on("click", "#addFilesDialog", function () {
        $("#choose-file").text("Välj fil");
    });

    $("body").on("click", "#addFilesDialog .upload-files", function (event) {
        event.preventDefault();
        //Before we start, validate the form!
        var $form = $(this).closest("form");
        if ($form.valid()) {
            var $size = 0;
            $('.file-table-add > tbody > tr').each(function () {
                $size += $(this).data("size");
            });

            var $files = $form.find("input[type=file]")[0].files;
            for (var i = 0; i < $files.length; ++i) {
                $size += $files[i].size;
            }
            if ($size > $("#CombinedMaxSizeAttachments").val()) {
                alert("Den totala storleken på alla bifogade filer överstiger den tillåtna gränsen " + parseInt($("#CombinedMaxSizeAttachments").val() / 1024 / 1024) + "MB");
                return;
            }
            $.ajax({
                url: $form[0].action,
                type: 'POST',
                data: new FormData($form[0]),
                processData: false,
                contentType: false,
                success: function (data) {
                    if (!data.success) {
                        alert(data.errorMessage, "Problem vid uppladdning");
                    } else {
                        for (var i = 0; i < data.fileInfo.length; ++i) {
                            var $hidden = $("#basefile").clone();
                            //Change the ids for the cloned inputs
                            $hidden.find("input").each(function () {
                                $(this).prop("id", $(this).prop("id").replace("0", currentId));
                                $(this).prop("name", $(this).prop("name").replace("0", currentId));
                            });
                            currentId++;
                            $hidden.find("input").val(data.fileInfo[i].id);
                            $('.file-table-add > tbody:last-child').append('<tr data-id="' + data.fileInfo[i].id + '" data-size="' + data.fileInfo[i].size + '">' +
                                '<td class="table-full-column"> <span class="glyphicon glyphicon-paperclip"></span>' + $hidden.html() + data.fileInfo[i].fileName + '</td>' +
                                '<td class="table-button-column fixed"><span class="remove-file-row bold">&times;</span></td>' +
                                '</tr>');
                            //Make the table visible, if this is the first visible row.
                            $('.file-list').removeClass("d-none");
                        }
                        $("#FileGroupKey").val(data.groupKey);
                        $("#GroupKey").val(data.groupKey);
                        $("#addFilesDialog").modal("hide");
                    }
                }
            });
        }
    });
    currentId = $(".file-table-add > tbody > tr").length;
});

$("body").on("change", ".filestyle", function () {
    var files = $(".filestyle")[0].files;
    if (files.length > 0) {
        var fileInfo = "";
        $("#choose-file").text("Ändra fil");
        for (var i = 0; i < files.length; i++) {
            fileInfo += i > 0 ? ", " + files[i].name : " " + files[i].name;
        }
    }
    $("#selected-filename").text(fileInfo);
});
