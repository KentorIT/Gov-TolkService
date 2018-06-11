$(function () {
    var currentId = 0;
    $("body").on("click", ".remove-requirement-row", function () {
        var $tbody = $(this).closest("tbody");
        $(this).closest("tr").remove();
        //Reindex 0 to n
        var $rows = $tbody.find("tr");
        currentId = 0;
        if ($rows.length === 0) {
            $('.order-requirement-list').addClass("d-none");
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
    $("body").on("click", ".add-requirement-button", function () {
        $($(this).data("target")).find("input:not(:checkbox),select, textarea").val("");
        $($(this).data("target")).find("input:checkbox").prop("checked", false);
        var $form = $($(this).data("target")).find('form:first');
        $($(this).data("target")).bindEnterKey('form:first input', '.btn-default');
        $form.find(".field-validation-error")
            .addClass("field-validation-valid")
            .removeClass("field-validation-error").html("");
    });
    $("body").on("click", ".save-requirement", function (event) {
        event.preventDefault();
        //Before we start, validate the form!
        if ($(this).parents(".modal-content").find("form").valid()) {
            var $hidden = $("#baseRequirement").clone();
            //Change the ids for the cloned inputs
            $hidden.find("input").each(function () {
                $(this).prop("id", $(this).prop("id").replace("0", currentId));
                $(this).prop("name", $(this).prop("name").replace("0", currentId));
            });
            currentId++;
            $(this).parents(".modal-content").find("input:not(:checkbox), select, textarea").each(function () {
                $hidden.find("input[name$='" + $(this).prop("id") + "']").val($(this).val());
            });
            $(this).parents(".modal-content").find("input:checkbox").each(function () {
                $hidden.find("input[name$='" + $(this).prop("id") + "']").val($(this).is(":checked") ? "true" : "false");
            });
            //Add the info to the cloned hidden fields.
            //add a row to the table
            var isRequired = $("#RequirementIsRequired").is(":checked") ? "Ja" : "Nej";
            $('.order-requirement-table > tbody:last-child').append('<tr>' +
                '<td class="table-type-column">' + $hidden.html() + $("#RequirementType option:selected").text() + '</td>' +
                '<td class="table-description-column">' + $("#RequirementDescription").val() + '</td>' +
                '<td class="table-is-required-column">' + isRequired + '</td>' +
                '<td class="table-button-column fixed"><span class="glyphicon glyphicon-trash remove-requirement-row"></span></td>' +
                '</tr>');
            //Make the table visible, if this is the first visible row.
            $('.order-requirement-list').removeClass("d-none");
            //Close dialog
            $("#addRequirement").modal("hide");
        }
    });
    $("body").on("change", "#UseRankedInterpreterLocation", function (event) {
        if ($(this).is(":checked")) {
            $(".required-InterpreterLocation").hide();
            $(".ranked-InterpreterLocation").show();
            $(".address-InterpreterLocation").show();
            $(".off-site-information").show();
        } else {
            $(".required-InterpreterLocation").show();
            $(".ranked-InterpreterLocation").hide();
            $("#InterpreterLocationSelector").trigger("change");
        }
    });
    $("body").on("change", "#AssignmentType", function (event) {
        if ($(this).val() === "OnSite" || $(this).val() === "OffSite" || $(this).val() === "OffSiteDesignatedLocation") {
            //Uncheck (with event) the UseRankedInterpreterLocation
            if ($("#UseRankedInterpreterLocation").is(":checked")) {
                $("#UseRankedInterpreterLocation").trigger("click");
            }
            //Disable UseRankedInterpreterLocation
            $("#UseRankedInterpreterLocation").prop("disabled", true);
            // Set InterpreterLocation, and make it disabled
            $("#InterpreterLocationSelector").val($(this).val());
            $("#InterpreterLocationSelector").prop("disabled", true);
        } else {
            //Enable UseRankedInterpreterLocation
            $("#UseRankedInterpreterLocation").prop("disabled", false);
            //Enable InterpreterLocation
            $("#InterpreterLocationSelector").val("");
            $("#InterpreterLocationSelector").prop("disabled", false);
        }
        $("#InterpreterLocationSelector").trigger("change");
    });
    $("body").on("change", "#InterpreterLocationSelector", function (event) {
        if ($(this).val() === null) {
            $(".address-InterpreterLocation").hide();
            // show offsite info
            $(".off-site-information").hide();
        }
        else if ($(this).val() === "OffSite") {
            //hide address
            // show offsite info
            $(".address-InterpreterLocation").hide();
            // show offsite info
            $(".off-site-information").show();
        } else {
            //show address
            $(".address-InterpreterLocation").show();
            $(".off-site-information").hide();
            // hide offsite info
        }
        //hidden field to propagate the value even if the select is disabled.
        $("#InterpreterLocation").val($(this).val());
    });
    $("ol.drag-panel").sortable({
        vertical: true,
        pullPlaceholder: true,
        group: 'draggable',
        distance: 20,

        // animation on drop
        onDrop: function ($item, container, _super) {
            var $clonedItem = $('<li/>').css({ width: 0, height: 0 });
            $item.before($clonedItem);
            $clonedItem.animate({ 'height': $item.height() });

            $item.animate($clonedItem.position(), function () {
                $clonedItem.detach();
                _super($item, container);
                //Rerank!
                var i = 1;
                $item.closest("ol").find(".order-descriptor").each(function () {
                    $(this).val(i++);
                });
            });
        },

        // set $item relative to cursor position
        onDragStart: function ($item, container, _super) {
            var offset = $item.offset(),
                pointer = container.rootGroup.pointer;

            adjustment = {
                left: pointer.left - offset.left,
                top: pointer.top - offset.top
            };

            _super($item, container);
        },
        onDrag: function ($item, position) {
            $item.css({
                left: position.left - adjustment.left,
                top: position.top - adjustment.top
            });
        }
    });
});
