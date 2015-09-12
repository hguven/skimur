﻿; skimurui.posts = (function () {

    var getPost = function (element) {
        return $(element).closest(".post");
    };

    var cancel = function (element) {

        var $post = getPost(element);

        // hide any content that may be staged (editing/banning/etc).
        var $staging = $post.find("> .disc-body .disc-staging").addClass("hidden").empty();

        return {
            post: $post,
            staging: $staging
        }
    };

    var voteUp = function (element) {

        if (!skimurui.login.checkLoggedIn("You must be logged in to vote."))
            return;

        var $post = getPost(element);
        var $voting = $(".disc-voting", $post);

        // the user wants to upvote a post!
        if ($voting.hasClass("vote-processing")) return;
        if ($voting.hasClass("voted-up")) {
            // the user already upvoted it, let's just remove the vote
            $voting.addClass("vote-processing");
            skimur.unvotePost($post.data("post-id"), function (result) {
                $voting.removeClass("vote-processing");
                if (result.success) {
                    var votes = $(".votes", $voting);
                    votes.html(+votes.html() - 1);
                    $voting.removeClass("voted-up").removeClass("voted-down");
                } else {
                    skimurui.displayError(result.error);
                }
            });
        } else {
            // the user hasn't casted an upvote, so lets add it
            $voting.addClass("vote-processing");
            skimur.upvotePost($post.data("post-id"), function (result) {
                $voting.removeClass("vote-processing");
                if (result.success) {
                    var votes = $(".votes", $voting);
                    votes.html(+votes.html() + 1 + ($voting.hasClass("voted-down") ? 1 : 0));
                    $voting.addClass("voted-up").removeClass("voted-down");
                } else {
                    skimurui.displayError(result.error);
                }
            });
        }
    };

    var voteDown = function (element) {

        if (!skimurui.login.checkLoggedIn("You must be logged in to vote."))
            return;

        var $post = getPost(element);
        var $voting = $(".disc-voting", $post);

        // the user wants to downvote a post!
        if ($voting.hasClass("vote-processing")) return;
        if ($voting.hasClass("voted-down")) {
            // the user already downvoted it, let's just remove the vote
            $voting.addClass("vote-processing");
            skimur.unvotePost($post.data("post-id"), function (result) {
                $voting.removeClass("vote-processing");
                if (result.success) {
                    var votes = $(".votes", $voting);
                    votes.html(+votes.html() + 1);
                    $voting.removeClass("voted-up").removeClass("voted-down");
                } else {
                    skimurui.displayError(result.error);
                }
            });
        } else {
            // the user hasn't casted a downvote, so lets add it
            $voting.addClass("vote-processing");
            skimur.downvotePost($post.data("post-id"), function (result) {
                $voting.removeClass("vote-processing");
                if (result.success) {
                    var votes = $(".votes", $voting);
                    votes.html(+votes.html() - 1 - ($voting.hasClass("voted-up") ? 1 : 0));
                    $voting.removeClass("voted-up").addClass("voted-down");
                } else {
                    skimurui.displayError(result.error);
                }
            });
        }
    };

    var approve = function(element) {
        var $post = getPost(element);
        skimurui.confirmInfo("Are you sure?", "Yes, approve it!", function (confirmResult) {
            if (confirmResult.confirmed) {
                skimur.approvePost($post.data("post-id"), function (result) {
                    if (result.success) {
                        skimurui.displaySuccess("The post has been approved.");
                        $post.removeClass("removed").addClass("approved");
                        $(".verdict", $post).removeClass("removed").addClass("approved");
                    } else {
                        skimurui.displayError(result.error);
                    }
                });
            }
        });
    };

    var remove = function (element) {
        var $post = getPost(element);
        skimurui.confirmWarning("Are you sure?", "Yes, remove it!", function(confirmResult) {
            if (confirmResult.confirmed) {
                skimur.removePost($post.data("post-id"), function (result) {
                    if (result.success) {
                        skimurui.displaySuccess("The post has been removed.");
                        $post.removeClass("approved").addClass("removed");
                        $(".verdict", $post).removeClass("approved").addClass("removed");
                    } else {
                        skimurui.displayError(result.error);
                    }
                });
            }
        });
    };

    var report = function (element) {

        if (!skimurui.login.checkLoggedIn("You must be logged in to report."))
            return;

        var post = cancel(element);

        var $form = skimurui.buildReportForm().appendTo(post.staging);

        $(".report", $form).click(function (e) {
            skimur.reportPost(post.post.data("post-id"), $("input[type='radio']:checked", $form).val(), $("input[type='text']", $form).val(), function (result) {
                console.log(result);
                if (result.success) {
                    skimurui.displaySuccess("The post has been reported.");
                    cancel(element);
                } else {
                    skimurui.displayError(result.error);
                }
            });
        });

        $(".cancel", $form).click(function (e) {
            cancel(element);
        });

        post.staging.removeClass("hidden");
    };

    return {
        voteUp: voteUp,
        voteDown: voteDown,
        approve: approve,
        remove: remove,
        report: report
    };

})();